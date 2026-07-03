namespace VideoThumbnailBrowser.Models;

/// <summary>
/// 動画・書庫を開くために登録された外部アプリケーション。
/// </summary>
public class ExternalApp
{
    /// <summary>表示名（例: "MPC-HC"、"Honeyview"）。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>実行ファイルのフルパス。</summary>
    public string ExePath { get; set; } = string.Empty;

    /// <summary>起動引数テンプレート。{0} がファイルパスに置換される。空の場合は単純にファイルパスを渡す。</summary>
    public string Arguments { get; set; } = "\"{0}\"";
}
