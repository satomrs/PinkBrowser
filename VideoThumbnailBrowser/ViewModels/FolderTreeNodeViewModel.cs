using System.Collections.ObjectModel;
using VideoThumbnailBrowser.Models;

namespace VideoThumbnailBrowser.ViewModels;

/// <summary>
/// フォルダツリーの1ノードのUI状態（展開状態・選択状態）を保持する。
/// </summary>
public class FolderTreeNodeViewModel : ViewModelBase
{
    public FolderTreeNode Model { get; }
    public string Name => Model.IsWatchedRoot ? Model.FullPath : Model.Name;
    public string FullPath => Model.FullPath;
    public bool IsWatchedRoot => Model.IsWatchedRoot;

    public ObservableCollection<FolderTreeNodeViewModel> Children { get; } = new();

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetField(ref _isExpanded, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    public FolderTreeNodeViewModel(FolderTreeNode model)
    {
        Model = model;
        IsExpanded = model.IsWatchedRoot;

        foreach (var child in model.Children)
            Children.Add(new FolderTreeNodeViewModel(child));
    }
}
