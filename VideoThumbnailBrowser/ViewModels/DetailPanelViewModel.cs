namespace VideoThumbnailBrowser.ViewModels;

/// <summary>
/// 右サイドの詳細パネルに表示する動画情報のViewModel。
/// サムネイルクリック時にMainViewModelがこのプロパティを更新する。
/// </summary>
public class DetailPanelViewModel : ViewModelBase
{
    private VideoItemViewModel? _item;
    public VideoItemViewModel? Item
    {
        get => _item;
        set
        {
            SetField(ref _item, value);
            OnPropertyChanged(nameof(IsVisible));
        }
    }

    public bool IsVisible => _item != null;

    private bool _isPanelOpen;
    public bool IsPanelOpen
    {
        get => _isPanelOpen;
        set => SetField(ref _isPanelOpen, value);
    }

    public void Show(VideoItemViewModel item)
    {
        Item = item;
        IsPanelOpen = true;
    }

    public void Close()
    {
        IsPanelOpen = false;
    }
}
