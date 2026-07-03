using System.Globalization;
using System.Windows.Data;

namespace VideoThumbnailBrowser;

/// <summary>
/// 2つの値が等しければ True を返す MultiValueConverter。
/// ページ番号ボタンで現在ページだけ色を変えるために使用する。
/// </summary>
public class EqualConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2) return false;
        return values[0]?.Equals(values[1]) ?? values[1] == null;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
