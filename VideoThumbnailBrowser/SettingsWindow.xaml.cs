using System.Windows;
using System.Windows.Forms;
using VideoThumbnailBrowser.Models;
using VideoThumbnailBrowser.ViewModels;

namespace VideoThumbnailBrowser;

public partial class SettingsWindow : Window
{
    public SettingsViewModel ViewModel { get; }

    public SettingsWindow(SettingsViewModel vm)
    {
        InitializeComponent();
        ViewModel = vm;
        DataContext = vm;

        // 動画アプリリスト（常に3枠）
        VideoAppList.ItemsSource = vm.VideoApps;
        ArchiveAppList.ItemsSource = vm.ArchiveApps;
    }

    private void OnBrowseAppClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn) return;
        if (btn.Tag is not ExternalApp app) return;

        using var dlg = new OpenFileDialog
        {
            Title = "実行ファイルを選択",
            Filter = "実行ファイル (*.exe)|*.exe|すべてのファイル (*.*)|*.*",
            CheckFileExists = true
        };

        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            app.ExePath = dlg.FileName;
            // TextBoxに反映（TwoWayバインディングが効かない場合の保険）
            VideoAppList.ItemsSource = null;
            VideoAppList.ItemsSource = ViewModel.VideoApps;
            ArchiveAppList.ItemsSource = null;
            ArchiveAppList.ItemsSource = ViewModel.ArchiveApps;
        }
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
