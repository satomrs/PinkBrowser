using System.Collections.ObjectModel;

namespace VideoThumbnailBrowser.ViewModels;

/// <summary>
/// サムネイル一覧を「列数ぶんのアイテムを1行にまとめたもの」として扱うための行データ。
///
/// WPF標準のVirtualizingStackPanelは「縦方向のスタック」しか仮想化できないため、
/// グリッド表示を実現するには行単位にグルーピングしてから縦に並べる、という構成にしている。
/// これにより数万件でも表示中の行だけが実体化され、スクロールが軽くなる。
/// </summary>
public class RowViewModel
{
    public ObservableCollection<VideoItemViewModel> Items { get; } = new();
}
