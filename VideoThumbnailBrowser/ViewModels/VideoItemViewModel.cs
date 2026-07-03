using System.IO;
using System.Windows.Media.Imaging;
using VideoThumbnailBrowser.Models;
using VideoThumbnailBrowser.Services;

namespace VideoThumbnailBrowser.ViewModels;

/// <summary>
/// 1本の動画に対するUI側のラッパー。
///
/// メモリ節約のため、サムネイル画像は「表紙（1枚目）」だけを先に読み込み、
/// マウスホバー時に初めて残りのスクラブ用フレームをまとめて読み込む。
/// </summary>
public class VideoItemViewModel : ViewModelBase
{
    public VideoItem Model { get; }
    private readonly ThumbnailCacheDb? _cacheDb;

    public string FileName => Path.GetFileName(Model.FilePath);
    public string FilePath => Model.FilePath;
    public string FolderPath => Path.GetDirectoryName(Model.FilePath) ?? string.Empty;
    public bool IsArchive => Model.Kind == Models.ItemKind.Archive;
    public string KindIcon => IsArchive ? "📦" : "🎬";

    /// <summary>ファイル名をTinySegmenterで分割したトークン一覧。サムネイル下部に表示する。</summary>
    public IReadOnlyList<string> FileNameTokens { get; }

    private static List<string> BuildTokens(string filePath)
    {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
        return TinySegmenter.TokenizeFileName(nameWithoutExt);
    }

    public string DurationText => TimeSpan.FromSeconds(Model.DurationSeconds).ToString(
        Model.DurationSeconds >= 3600 ? @"h\:mm\:ss" : @"m\:ss");

    public double DurationSeconds => Model.DurationSeconds;
    public long FileSize => Model.FileSize;
    public string FileSizeText => FormatFileSize(Model.FileSize);
    public int TagCount => Model.Tags.Count;
    public DateTime LastWriteTime =>
        Model.LastWriteTicks > 0
            ? new DateTime(Model.LastWriteTicks, DateTimeKind.Utc).ToLocalTime()
            : DateTime.MinValue;

    private int _playCount;
    public int PlayCount
    {
        get => _playCount;
        private set => SetField(ref _playCount, value);
    }

    public DateTime RegisteredAt =>
        Model.RegisteredTicks > 0
            ? new DateTime(Model.RegisteredTicks, DateTimeKind.Utc).ToLocalTime()
            : DateTime.MinValue;

    public string RegisteredAtText =>
        Model.RegisteredTicks > 0 ? RegisteredAt.ToString("yyyy/MM/dd HH:mm") : "-";

    public int FrameCount => Model.ThumbnailPaths.Count;

    private bool _isSelected;
    /// <summary>サムネイル一覧上での選択状態（チェックマーク表示・一括タグ付与の対象）。</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    private int _rating;
    /// <summary>星評価（0〜5）。設定すると即座にDBへ保存される。</summary>
    public int Rating
    {
        get => _rating;
        set
        {
            var clamped = Math.Clamp(value, 0, 5);
            if (SetField(ref _rating, clamped))
            {
                Model.Rating = clamped;
                _cacheDb?.SetRating(Model.FilePath, clamped);
            }
        }
    }

    /// <summary>表示・検索用のタグ一覧（カンマ区切り表示文字列）。</summary>
    public string TagsText => string.Join(", ", Model.Tags);

    public IReadOnlyList<string> Tags => Model.Tags;

    /// <summary>タグをまとめて置き換える。新規タグ名は自動でDBに作成される。</summary>
    public void SetTags(IEnumerable<string> tagNames)
    {
        var list = tagNames.Select(t => t.Trim()).Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        Model.Tags = list;
        _cacheDb?.SetTags(Model.FilePath, list);
        OnPropertyChanged(nameof(TagsText));
        OnPropertyChanged(nameof(Tags));
    }

    private BitmapImage? _coverImage;
    public BitmapImage? CoverImage
    {
        get => _coverImage;
        private set => SetField(ref _coverImage, value);
    }

    private readonly List<BitmapImage?> _scrubFrames;
    private bool _scrubFramesLoaded;

    public VideoItemViewModel(VideoItem model, ThumbnailCacheDb? cacheDb = null)
    {
        Model = model;
        _cacheDb = cacheDb;
        _rating = model.Rating;
        _playCount = model.PlayCount;
        FileNameTokens = BuildTokens(model.FilePath);
        _scrubFrames = new List<BitmapImage?>(new BitmapImage?[model.ThumbnailPaths.Count]);
    }

    /// <summary>動画を再生するときに呼び出す。再生回数をDBに記録する。</summary>
    public void IncrementPlayCount()
    {
        Model.PlayCount++;
        PlayCount = Model.PlayCount;
        _cacheDb?.IncrementPlayCount(Model.FilePath);
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes >= 1_073_741_824) return $"{bytes / 1_073_741_824.0:F1} GB";
        if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F1} MB";
        if (bytes >= 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes} B";
    }

    public void LoadCoverThumbnail()
    {
        if (CoverImage != null || Model.ThumbnailPaths.Count == 0) return;
        CoverImage = LoadBitmap(Model.ThumbnailPaths[0]);
    }

    /// <summary>マウスホバー時に呼び出し、スクラブ用の全フレームを遅延読み込みする。</summary>
    public void EnsureScrubFramesLoaded()
    {
        if (_scrubFramesLoaded) return;

        for (var i = 0; i < Model.ThumbnailPaths.Count; i++)
            _scrubFrames[i] = LoadBitmap(Model.ThumbnailPaths[i]);

        _scrubFramesLoaded = true;
    }

    public BitmapImage? GetFrame(int index)
    {
        if (index < 0 || index >= _scrubFrames.Count) return CoverImage;
        return _scrubFrames[index] ?? CoverImage;
    }

    private static BitmapImage? LoadBitmap(string path)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.DecodePixelWidth = 320;
            bitmap.EndInit();
            bitmap.Freeze(); // UIスレッド以外からも安全に参照できるようにする
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}
