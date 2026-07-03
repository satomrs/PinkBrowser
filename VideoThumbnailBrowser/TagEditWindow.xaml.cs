using System.Windows;
using System.Windows.Controls;

namespace VideoThumbnailBrowser;

public partial class TagEditWindow : Window
{
    /// <summary>OK押下後に確定したタグ一覧。</summary>
    public List<string> ResultTags { get; private set; } = new();

    public TagEditWindow(IEnumerable<string> currentTags, IEnumerable<string> existingTagSuggestions)
    {
        InitializeComponent();
        TagInputBox.Text = string.Join(", ", currentTags);
        ExistingTagsControl.ItemsSource = existingTagSuggestions.ToList();
    }

    private void OnExistingTagClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Content is not string tagName) return;

        var current = SplitInput(TagInputBox.Text);
        if (!current.Contains(tagName, StringComparer.OrdinalIgnoreCase))
            current.Add(tagName);

        TagInputBox.Text = string.Join(", ", current);
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        ResultTags = SplitInput(TagInputBox.Text);
        DialogResult = true;
    }

    private static List<string> SplitInput(string text) =>
        text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
