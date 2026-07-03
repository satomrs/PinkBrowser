using VideoThumbnailBrowser.Models;

namespace VideoThumbnailBrowser.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private int _thumbnailsPerVideo;
    public int ThumbnailsPerVideo
    {
        get => _thumbnailsPerVideo;
        set => SetField(ref _thumbnailsPerVideo, Math.Clamp(value, 1, 30));
    }

    private int _thumbnailDisplayWidth;
    public int ThumbnailDisplayWidth
    {
        get => _thumbnailDisplayWidth;
        set => SetField(ref _thumbnailDisplayWidth, Math.Clamp(value, 80, 480));
    }

    /// <summary>動画用起動ソフト（常に3枠、空欄は無効）。</summary>
    public List<ExternalApp> VideoApps { get; }

    /// <summary>書庫用起動ソフト（常に3枠、空欄は無効）。</summary>
    public List<ExternalApp> ArchiveApps { get; }

    public SettingsViewModel(int thumbnailsPerVideo, int thumbnailDisplayWidth,
        List<ExternalApp> videoApps, List<ExternalApp> archiveApps)
    {
        _thumbnailsPerVideo = thumbnailsPerVideo;
        _thumbnailDisplayWidth = thumbnailDisplayWidth;

        // 常に3枠を確保（足りなければ空エントリを追加）
        VideoApps = PadToThree(videoApps, "動画");
        ArchiveApps = PadToThree(archiveApps, "書庫");
    }

    private static List<ExternalApp> PadToThree(List<ExternalApp> source, string prefix)
    {
        var result = source.Select(a => new ExternalApp
            { Name = a.Name, ExePath = a.ExePath, Arguments = a.Arguments }).ToList();

        for (var i = result.Count + 1; i <= 3; i++)
            result.Add(new ExternalApp { Name = $"{prefix}ソフト{i}", ExePath = "" });

        return result.Take(3).ToList();
    }

    /// <summary>空でないエントリだけ返す（設定保存用）。</summary>
    public List<ExternalApp> GetValidVideoApps() =>
        VideoApps.Where(a => !string.IsNullOrWhiteSpace(a.ExePath)).ToList();

    public List<ExternalApp> GetValidArchiveApps() =>
        ArchiveApps.Where(a => !string.IsNullOrWhiteSpace(a.ExePath)).ToList();
}
