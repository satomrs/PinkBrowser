using System.Collections.ObjectModel;

namespace VideoThumbnailBrowser.Models;

/// <summary>
/// 左サイドバーのフォルダツリーの1ノード。
/// 監視フォルダ自体がルートになり、その配下の実フォルダ構造を反映する。
/// </summary>
public class FolderTreeNode
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;

    /// <summary>このノードが監視フォルダそのもの（ルート）かどうか。</summary>
    public bool IsWatchedRoot { get; set; }

    public ObservableCollection<FolderTreeNode> Children { get; } = new();
}
