namespace VideoThumbnailBrowser.Models;

/// <summary>
/// 1つのDBプロファイル（独立したDB・サムネイルキャッシュ・監視フォルダセットを持つ）。
/// </summary>
public class DbProfile
{
    /// <summary>表示名（例: "仕事用"、"プライベート"）。</summary>
    public string Name { get; set; } = "Default";

    /// <summary>このプロファイルのデータが格納されるサブディレクトリ名。</summary>
    public string DirectoryName { get; set; } = "Default";
}
