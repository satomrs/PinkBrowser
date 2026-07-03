using System.IO;
using System.Threading;

namespace VideoThumbnailBrowser.Services;

/// <summary>
/// 複数フォルダをFileSystemWatcherで監視し、動画ファイルの追加・削除・リネームを
/// UIスレッドへ安全に通知する。
///
/// 「追加」イベントは、動画ファイルの書き込みが完了するまで（コピー中でなくなるまで）
/// 待ってから発火させる。これによりコピー途中の不完全なファイルにサムネイル生成を
/// 試みてしまう問題を避けている。
/// </summary>
public class FolderWatcherService : IDisposable
{
    private readonly List<FileSystemWatcher> _watchers = new();
    private readonly SynchronizationContext? _syncContext;

    public event Action<string>? FileAdded;
    public event Action<string>? FileRemoved;
    public event Action<string, string>? FileRenamed; // (oldPath, newPath)

    public FolderWatcherService()
    {
        // UIスレッドで生成されることを前提に、Dispatcher同期コンテキストを保持する。
        _syncContext = SynchronizationContext.Current;
    }

    public void Watch(Models.WatchedFolder folder)
    {
        if (!Directory.Exists(folder.Path)) return;

        var watcher = new FileSystemWatcher(folder.Path)
        {
            IncludeSubdirectories = folder.Recursive,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
        };

        watcher.Created += (_, e) => OnCreatedOrChanged(e.FullPath);
        watcher.Changed += (_, e) => OnCreatedOrChanged(e.FullPath);
        watcher.Deleted += (_, e) => OnDeleted(e.FullPath);
        watcher.Renamed += (_, e) => OnRenamed(e.OldFullPath, e.FullPath);
        watcher.Error += (_, _) =>
        {
            // 監視バッファのオーバーフローなどで停止した場合は再起動する。
            try
            {
                watcher.EnableRaisingEvents = false;
                watcher.EnableRaisingEvents = true;
            }
            catch
            {
                // フォルダが削除された等で再起動できない場合は諦める。
            }
        };

        watcher.EnableRaisingEvents = true;
        _watchers.Add(watcher);
    }

    public void UnwatchAll()
    {
        foreach (var w in _watchers)
        {
            try
            {
                w.EnableRaisingEvents = false;
            }
            catch
            {
                // ignore
            }
            w.Dispose();
        }
        _watchers.Clear();
    }

    private async void OnCreatedOrChanged(string path)
    {
        if (!ArchiveFileTypes.IsVideoOrArchive(path)) return;
        var ready = await WaitUntilFileReadyAsync(path).ConfigureAwait(false);
        if (!ready) return;
        Raise(() => FileAdded?.Invoke(path));
    }

    private void OnDeleted(string path)
    {
        if (!ArchiveFileTypes.IsVideoOrArchive(path)) return;
        Raise(() => FileRemoved?.Invoke(path));
    }

    private void OnRenamed(string oldPath, string newPath)
    {
        var oldIs = ArchiveFileTypes.IsVideoOrArchive(oldPath);
        var newIs = ArchiveFileTypes.IsVideoOrArchive(newPath);

        if (oldIs && newIs) Raise(() => FileRenamed?.Invoke(oldPath, newPath));
        else if (newIs) OnCreatedOrChanged(newPath);
        else if (oldIs) Raise(() => FileRemoved?.Invoke(oldPath));
    }

    /// <summary>
    /// ファイルサイズが2回連続で同じ値になるまで（＝書き込みが落ち着くまで）待つ。
    /// 最大60秒でタイムアウトする。
    /// </summary>
    private static async Task<bool> WaitUntilFileReadyAsync(string path, int timeoutMs = 60000)
    {
        var start = Environment.TickCount;
        long lastSize = -1;

        while (Environment.TickCount - start < timeoutMs)
        {
            if (!File.Exists(path)) return false;

            try
            {
                using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var size = stream.Length;
                if (size == lastSize && size > 0) return true;
                lastSize = size;
            }
            catch (IOException)
            {
                // まだ書き込み元プロセスにロックされている。待機を続ける。
            }

            await Task.Delay(500).ConfigureAwait(false);
        }

        return false;
    }

    private void Raise(Action action)
    {
        if (_syncContext != null)
            _syncContext.Post(_ => action(), null);
        else
            action();
    }

    public void Dispose() => UnwatchAll();
}
