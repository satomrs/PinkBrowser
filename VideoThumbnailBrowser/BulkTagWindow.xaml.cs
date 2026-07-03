using System.Windows;
using System.Windows.Controls;

namespace VideoThumbnailBrowser;

public partial class BulkTagWindow : Window
{
    /// <summary>OK後に一括付与するタグ名一覧（重複なし）。</summary>
    public List<string> TagsToAdd { get; private set; } = new();

    // クリックで追加した既存タグを一時保持する。
    private readonly HashSet<string> _selectedTags = new(StringComparer.OrdinalIgnoreCase);

    public BulkTagWindow(int targetCount, IEnumerable<string> existingTagSuggestions)
    {
        InitializeComponent();
        TargetCountText.Text = $"{targetCount} 件の動画にタグを追加します";
        ExistingTagsControl.ItemsSource = existingTagSuggestions.ToList();
    }

    private void OnTagButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn) return;
        if (btn.Content is not string tagName) return;

        if (_selectedTags.Contains(tagName))
        {
            // 既に選んでいる場合は解除（トグル）
            _selectedTags.Remove(tagName);
            btn.Background = System.Windows.Media.Brushes.Transparent;
            btn.BorderBrush = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#555"));
        }
        else
        {
            _selectedTags.Add(tagName);
            btn.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#007ACC"));
            btn.BorderBrush = System.Windows.Media.Brushes.Transparent;
        }

        RefreshSelectedTagsDisplay();
    }

    private void RefreshSelectedTagsDisplay()
    {
        var all = GetAllPendingTags();
        if (all.Count > 0)
        {
            SelectedTagsPanel.Visibility = Visibility.Visible;
            SelectedTagsText.Text = string.Join(", ", all);
        }
        else
        {
            SelectedTagsPanel.Visibility = Visibility.Collapsed;
        }
    }

    private List<string> GetAllPendingTags()
    {
        // テキスト入力 + クリック選択 を合わせた重複なしリスト。
        var fromInput = NewTagInput.Text
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return _selectedTags
            .Concat(fromInput)
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void OnApplyClick(object sender, RoutedEventArgs e)
    {
        TagsToAdd = GetAllPendingTags();
        if (TagsToAdd.Count == 0)
        {
            // 何も選んでいなければそのまま閉じない（誤操作防止）
            System.Windows.MessageBox.Show(
                "付与するタグを選択または入力してください。",
                "タグが未選択", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        DialogResult = true;
    }
}
