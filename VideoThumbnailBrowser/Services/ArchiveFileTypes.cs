using System.IO;

namespace VideoThumbnailBrowser.Services;

public static class ArchiveFileTypes
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".zip", ".cbz", ".rar", ".cbr", ".7z", ".cb7",
        ".tar", ".gz", ".bz2", ".lzh", ".lha"
    };

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".avif"
    };

    public static bool IsArchiveFile(string path)
    {
        var ext = Path.GetExtension(path);
        return !string.IsNullOrEmpty(ext) && Extensions.Contains(ext);
    }

    public static bool IsImageFile(string name)
    {
        var ext = Path.GetExtension(name);
        return !string.IsNullOrEmpty(ext) && ImageExtensions.Contains(ext);
    }

    public static bool IsVideoOrArchive(string path) =>
        VideoFileTypes.IsVideoFile(path) || IsArchiveFile(path);
}
