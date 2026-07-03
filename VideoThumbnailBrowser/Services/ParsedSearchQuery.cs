namespace VideoThumbnailBrowser.Services;

/// <summary>
/// 検索ボックスの文字列を解析する。
/// 通常の単語はファイル名の部分一致、"tag:xxx" はタグの部分一致、
/// "rating:N" や "rating:>=N" は評価の比較に使う。
/// 複数の条件はすべてAND条件として扱う。
/// </summary>
public class ParsedSearchQuery
{
    public List<string> NameKeywords { get; } = new();
    public List<string> TagKeywords { get; } = new();
    public (string op, int value)? RatingFilter { get; private set; }

    public static ParsedSearchQuery Parse(string input)
    {
        var query = new ParsedSearchQuery();
        if (string.IsNullOrWhiteSpace(input)) return query;

        var tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var token in tokens)
        {
            if (token.StartsWith("tag:", StringComparison.OrdinalIgnoreCase))
            {
                var value = token[4..];
                if (value.Length > 0) query.TagKeywords.Add(value);
            }
            else if (token.StartsWith("rating:", StringComparison.OrdinalIgnoreCase))
            {
                ParseRating(token[7..], query);
            }
            else
            {
                query.NameKeywords.Add(token);
            }
        }

        return query;
    }

    private static void ParseRating(string expr, ParsedSearchQuery query)
    {
        string op = "=";
        var numberPart = expr;

        if (expr.StartsWith(">=")) { op = ">="; numberPart = expr[2..]; }
        else if (expr.StartsWith("<=")) { op = "<="; numberPart = expr[2..]; }
        else if (expr.StartsWith(">")) { op = ">"; numberPart = expr[1..]; }
        else if (expr.StartsWith("<")) { op = "<"; numberPart = expr[1..]; }

        if (int.TryParse(numberPart, out var value))
        {
            query.RatingFilter = (op, Math.Clamp(value, 0, 5));
        }
    }

    public bool IsEmpty => NameKeywords.Count == 0 && TagKeywords.Count == 0 && RatingFilter == null;
}
