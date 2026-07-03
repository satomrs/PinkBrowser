using System.IO;

namespace VideoThumbnailBrowser.Services;

/// <summary>
/// 指定フォルダ以下の動画・書庫ファイルを列挙する。
/// ファイル列挙とサブフォルダ列挙を独立したtry-catchで囲み、
/// 一方が失敗してももう一方に影響しない。深い階層でも取りこぼしがない。
/// </summary>
public static class VideoScanner
{
    public static IEnumerable<string> EnumerateVideoFiles(string rootFolder, bool recursive)
    {
        var pending = new Stack<string>();
        pending.Push(rootFolder);

        while (pending.Count > 0)
        {
            var current = pending.Pop();

            // ファイル列挙（失敗しても続行）
            IEnumerable<string> files = Array.Empty<string>();
            try
            {
                files = Directory.GetFiles(current); // EnumerateFilesより確実（全件取得してからyield）
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
            catch (Exception) { }

            foreach (var file in files)
            {
                if (ArchiveFileTypes.IsVideoOrArchive(file))
                    yield return file;
            }

            // サブフォルダ列挙（ファイル列挙の失敗と独立）
            if (!recursive) continue;

            IEnumerable<string> subDirs = Array.Empty<string>();
            try
            {
                subDirs = Directory.GetDirectories(current);
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
            catch (Exception) { }

            foreach (var dir in subDirs)
                pending.Push(dir);
        }
    }
}
