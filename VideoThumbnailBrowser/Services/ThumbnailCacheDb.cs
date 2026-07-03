using System.IO;
using Microsoft.Data.Sqlite;
using VideoThumbnailBrowser.Models;

namespace VideoThumbnailBrowser.Services;

/// <summary>
/// 動画ファイルパス → サムネイル情報・評価・タグ の対応をSQLiteに永続化する。
/// ファイルサイズと最終更新日時が変わっていなければ、サムネイルを再生成せずに済む。
///
/// VideoCache : 1動画につき1行（サムネイル・評価などのスカラー値）
/// Tags       : タグ名のマスタ（重複なし）
/// VideoTags  : 動画とタグの多対多の関連
/// </summary>
public class ThumbnailCacheDb
{
    private readonly string _connectionString;

    public ThumbnailCacheDb(string dbPath)
    {
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        _connectionString = $"Data Source={dbPath}";
        Initialize();
    }

    private void Initialize()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS VideoCache (
                FilePath TEXT PRIMARY KEY,
                FileSize INTEGER NOT NULL,
                LastWriteTicks INTEGER NOT NULL,
                DurationSeconds REAL NOT NULL,
                ThumbnailPaths TEXT NOT NULL,
                Rating INTEGER NOT NULL DEFAULT 0,
                PlayCount INTEGER NOT NULL DEFAULT 0,
                RegisteredTicks INTEGER NOT NULL DEFAULT 0,
                Kind INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS Tags (
                TagId INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE COLLATE NOCASE
            );
            CREATE TABLE IF NOT EXISTS VideoTags (
                FilePath TEXT NOT NULL,
                TagId INTEGER NOT NULL,
                PRIMARY KEY (FilePath, TagId),
                FOREIGN KEY (TagId) REFERENCES Tags(TagId) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS IX_VideoTags_TagId ON VideoTags(TagId);
            """;
        cmd.ExecuteNonQuery();

        EnsureColumnExists(conn, "VideoCache", "Rating", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumnExists(conn, "VideoCache", "PlayCount", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumnExists(conn, "VideoCache", "RegisteredTicks", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumnExists(conn, "VideoCache", "Kind", "INTEGER NOT NULL DEFAULT 0");
    }

    private static void EnsureColumnExists(SqliteConnection conn, string table, string column, string definition)
    {
        using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = $"PRAGMA table_info({table});";
        using var reader = checkCmd.ExecuteReader();
        while (reader.Read())
        {
            var name = reader.GetString(1);
            if (string.Equals(name, column, StringComparison.OrdinalIgnoreCase))
                return; // 既に存在する
        }
        reader.Close();

        using var alterCmd = conn.CreateCommand();
        alterCmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition};";
        alterCmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 起動時に一括で全件読み込み、以後はメモリ上の辞書で照合する用途。
    /// ファイル数が多い場合、1件ずつSELECTするより大幅に速い。
    /// タグも一括JOINして読み込み、N+1クエリを避ける。
    /// </summary>
    public Dictionary<string, VideoItem> LoadAll()
    {
        var result = new Dictionary<string, VideoItem>(StringComparer.OrdinalIgnoreCase);

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT FilePath, FileSize, LastWriteTicks, DurationSeconds, ThumbnailPaths, Rating, PlayCount, RegisteredTicks, Kind FROM VideoCache";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var path = reader.GetString(0);
                var thumbsJoined = reader.GetString(4);
                var thumbs = thumbsJoined.Length == 0
                    ? new List<string>() : thumbsJoined.Split('|').ToList();
                result[path] = new VideoItem
                {
                    FilePath = path,
                    FileSize = reader.GetInt64(1),
                    LastWriteTicks = reader.GetInt64(2),
                    DurationSeconds = reader.GetDouble(3),
                    ThumbnailPaths = thumbs,
                    Rating = reader.GetInt32(5),
                    PlayCount = reader.GetInt32(6),
                    RegisteredTicks = reader.GetInt64(7),
                    Kind = (Models.ItemKind)reader.GetInt32(8)
                };
            }
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT vt.FilePath, t.Name
                FROM VideoTags vt
                JOIN Tags t ON t.TagId = vt.TagId
                """;
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var path = reader.GetString(0);
                var tagName = reader.GetString(1);
                if (result.TryGetValue(path, out var item))
                    item.Tags.Add(tagName);
            }
        }

        return result;
    }

    public void Upsert(VideoItem item)
    {
        if (item.RegisteredTicks == 0)
            item.RegisteredTicks = DateTime.UtcNow.Ticks;

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO VideoCache (FilePath, FileSize, LastWriteTicks, DurationSeconds, ThumbnailPaths, Rating, PlayCount, RegisteredTicks, Kind)
            VALUES ($path, $size, $ticks, $duration, $thumbs, $rating, $playCount, $registeredTicks, $kind)
            ON CONFLICT(FilePath) DO UPDATE SET
                FileSize = $size,
                LastWriteTicks = $ticks,
                DurationSeconds = $duration,
                ThumbnailPaths = $thumbs,
                Kind = $kind;
            """;
        cmd.Parameters.AddWithValue("$path", item.FilePath);
        cmd.Parameters.AddWithValue("$size", item.FileSize);
        cmd.Parameters.AddWithValue("$ticks", item.LastWriteTicks);
        cmd.Parameters.AddWithValue("$duration", item.DurationSeconds);
        cmd.Parameters.AddWithValue("$thumbs", string.Join("|", item.ThumbnailPaths));
        cmd.Parameters.AddWithValue("$rating", item.Rating);
        cmd.Parameters.AddWithValue("$playCount", item.PlayCount);
        cmd.Parameters.AddWithValue("$registeredTicks", item.RegisteredTicks);
        cmd.Parameters.AddWithValue("$kind", (int)item.Kind);
        cmd.ExecuteNonQuery();
    }

    public void Delete(string filePath)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM VideoTags WHERE FilePath = $path";
            cmd.Parameters.AddWithValue("$path", filePath);
            cmd.ExecuteNonQuery();
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM VideoCache WHERE FilePath = $path";
            cmd.Parameters.AddWithValue("$path", filePath);
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>ファイルパスが変わった場合（リネーム検出時）に関連レコードを付け替える。</summary>
    public void RenamePath(string oldPath, string newPath)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "UPDATE VideoCache SET FilePath = $newPath WHERE FilePath = $oldPath";
            cmd.Parameters.AddWithValue("$newPath", newPath);
            cmd.Parameters.AddWithValue("$oldPath", oldPath);
            cmd.ExecuteNonQuery();
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "UPDATE VideoTags SET FilePath = $newPath WHERE FilePath = $oldPath";
            cmd.Parameters.AddWithValue("$newPath", newPath);
            cmd.Parameters.AddWithValue("$oldPath", oldPath);
            cmd.ExecuteNonQuery();
        }
    }

    public void IncrementPlayCount(string filePath)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE VideoCache SET PlayCount = PlayCount + 1 WHERE FilePath = $path";
        cmd.Parameters.AddWithValue("$path", filePath);
        cmd.ExecuteNonQuery();
    }

    public void SetRating(string filePath, int rating)
    {
        rating = Math.Clamp(rating, 0, 5);

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE VideoCache SET Rating = $rating WHERE FilePath = $path";
        cmd.Parameters.AddWithValue("$rating", rating);
        cmd.Parameters.AddWithValue("$path", filePath);
        cmd.ExecuteNonQuery();
    }

    /// <summary>登録済みの全タグ名を、よく使われる順に取得する（タグ入力時の候補表示用）。</summary>
    public List<string> GetAllTagNames()
    {
        var result = new List<string>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT t.Name
            FROM Tags t
            LEFT JOIN VideoTags vt ON vt.TagId = t.TagId
            GROUP BY t.TagId
            ORDER BY COUNT(vt.FilePath) DESC, t.Name COLLATE NOCASE ASC
            """;
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(reader.GetString(0));
        return result;
    }

    /// <summary>
    /// 指定動画のタグを丸ごと置き換える（追加・削除を1回のトランザクションで処理）。
    /// 新規タグ名はTagsテーブルに自動作成する。
    /// </summary>
    public void SetTags(string filePath, IEnumerable<string> tagNames)
    {
        var normalized = tagNames
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var transaction = conn.BeginTransaction();

        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = transaction;
            cmd.CommandText = "DELETE FROM VideoTags WHERE FilePath = $path";
            cmd.Parameters.AddWithValue("$path", filePath);
            cmd.ExecuteNonQuery();
        }

        foreach (var tagName in normalized)
        {
            long tagId;

            using (var insertTagCmd = conn.CreateCommand())
            {
                insertTagCmd.Transaction = transaction;
                insertTagCmd.CommandText = """
                    INSERT INTO Tags (Name) VALUES ($name)
                    ON CONFLICT(Name) DO UPDATE SET Name = Name
                    RETURNING TagId;
                    """;
                insertTagCmd.Parameters.AddWithValue("$name", tagName);
                tagId = (long)insertTagCmd.ExecuteScalar()!;
            }

            using (var linkCmd = conn.CreateCommand())
            {
                linkCmd.Transaction = transaction;
                linkCmd.CommandText = """
                    INSERT INTO VideoTags (FilePath, TagId) VALUES ($path, $tagId)
                    ON CONFLICT(FilePath, TagId) DO NOTHING;
                    """;
                linkCmd.Parameters.AddWithValue("$path", filePath);
                linkCmd.Parameters.AddWithValue("$tagId", tagId);
                linkCmd.ExecuteNonQuery();
            }
        }

        transaction.Commit();
    }
}
