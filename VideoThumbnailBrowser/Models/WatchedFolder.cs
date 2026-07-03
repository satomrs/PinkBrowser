namespace VideoThumbnailBrowser.Models;

/// <summary>
/// ユーザーが監視対象として登録した1つのフォルダ。
/// </summary>
public class WatchedFolder
{
    public string Path { get; set; } = string.Empty;

    /// <summary>サブフォルダも再帰的に監視するかどうか。</summary>
    public bool Recursive { get; set; } = true;
}
