using System.IO;
using VideoThumbnailBrowser.Models;

namespace VideoThumbnailBrowser.Services;

/// <summary>
/// 監視フォルダを起点に、実際のディスク上のサブフォルダ構造をツリーとして構築する。
/// アクセス権のないサブフォルダは無視して進む。
/// </summary>
public static class FolderTreeBuilder
{
    public static FolderTreeNode BuildTree(WatchedFolder folder)
    {
        var root = new FolderTreeNode
        {
            Name = folder.Path,
            FullPath = folder.Path,
            IsWatchedRoot = true
        };

        if (folder.Recursive)
            AddChildren(root);

        return root;
    }

    private static void AddChildren(FolderTreeNode node)
    {
        IEnumerable<string> subDirs;
        try
        {
            subDirs = Directory.EnumerateDirectories(node.FullPath).OrderBy(d => d, StringComparer.OrdinalIgnoreCase);
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }
        catch (IOException)
        {
            return;
        }

        foreach (var dir in subDirs)
        {
            var child = new FolderTreeNode
            {
                Name = Path.GetFileName(dir),
                FullPath = dir
            };
            node.Children.Add(child);
            AddChildren(child);
        }
    }
}
