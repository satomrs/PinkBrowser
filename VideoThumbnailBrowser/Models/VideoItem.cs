namespace VideoThumbnailBrowser.Models;

public enum ItemKind { Video, Archive }

/// <summary>
/// 1本の動画または1つの書庫ファイルとそのサムネイル情報。
/// </summary>
public class VideoItem
{
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public long LastWriteTicks { get; set; }
    public double DurationSeconds { get; set; }
    public List<string> ThumbnailPaths { get; set; } = new();
    public int Rating { get; set; }
    public List<string> Tags { get; set; } = new();
    public int PlayCount { get; set; }
    public long RegisteredTicks { get; set; }

    /// <summary>動画ファイルか書庫ファイルかを示す種別。</summary>
    public ItemKind Kind { get; set; } = ItemKind.Video;
}
