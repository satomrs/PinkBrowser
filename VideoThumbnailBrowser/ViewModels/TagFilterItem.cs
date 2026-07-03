namespace VideoThumbnailBrowser.ViewModels;

/// <summary>
/// タグフィルタードロップダウンの1行分（タグ名 + チェック状態）。
/// チェック状態が変わったら IsCheckedChanged イベントで MainViewModel に通知する。
/// </summary>
public class TagFilterItem : ViewModelBase
{
    public string Name { get; }
    /// <summary>表示名（タグ全体）。</summary>
    public string FullName { get; }
    /// <summary>スラッシュ前のグループ名（なければ空文字）。</summary>
    public string Group { get; }
    /// <summary>グループ内での短い名前。</summary>
    public string ShortName { get; }

    public event Action? IsCheckedChanged;

    private bool _isChecked;
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (SetField(ref _isChecked, value))
                IsCheckedChanged?.Invoke();
        }
    }

    public TagFilterItem(string name, bool isChecked = false)
    {
        FullName = name;
        Name = name;
        var slash = name.IndexOf('/');
        if (slash >= 0)
        {
            Group = name[..slash];
            ShortName = name[(slash + 1)..];
        }
        else
        {
            Group = string.Empty;
            ShortName = name;
        }
        _isChecked = isChecked;
    }
}
