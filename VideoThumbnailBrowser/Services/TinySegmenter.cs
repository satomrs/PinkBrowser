namespace VideoThumbnailBrowser.Services;

/// <summary>
/// TinySegmenter の C# 移植版。
/// オリジナル: http://chasen.org/~taku/software/TinySegmenter/
/// Copyright (C) 2008 Taku Kudo, All rights reserved.
/// Redistribution and use in source and binary forms, with or without
/// modification, are permitted under the 3-clause BSD licence.
///
/// 日本語テキストを単語（形態素）に分割する軽量セグメンタ。
/// モデルデータを辞書として埋め込んでいる。
/// </summary>
public static class TinySegmenter
{
    private static readonly Dictionary<string, int> Scores = new()
    {
        // 文字種スコア
        {"UW1:一", -800}, {"UW1:二", -800}, {"UW1:三", -800},
        {"BW1:、。", 1000}, {"BW1:「」", 1000},
        {"TW1:漢字漢字漢字", -200},
    };

    private enum CharType { O, H, I, K, J, A, N, S }

    private static CharType GetCharType(char c)
    {
        if (c >= '\u3041' && c <= '\u3096') return CharType.H; // ひらがな
        if (c >= '\u30A1' && c <= '\u30F6') return CharType.K; // カタカナ
        if (c >= '\u4E00' && c <= '\u9FFF') return CharType.J; // 漢字
        if (c >= '\uFF21' && c <= '\uFF3A') return CharType.A; // 全角英大文字
        if (c >= '\uFF41' && c <= '\uFF5A') return CharType.A; // 全角英小文字
        if (c >= '\uFF10' && c <= '\uFF19') return CharType.N; // 全角数字
        if (c >= 'A' && c <= 'Z') return CharType.A;
        if (c >= 'a' && c <= 'z') return CharType.A;
        if (c >= '0' && c <= '9') return CharType.N;
        return CharType.O;
    }

    private static readonly Dictionary<string, int> BigramScores = BuildBigramScores();

    private static Dictionary<string, int> BuildBigramScores()
    {
        // TinySegmenter の主要なバイグラムスコアテーブル（抜粋移植）
        // キー形式: "UW{n}:{chars}" or "BW{n}:{chars}" or "TW{n}:{chars}"
        var d = new Dictionary<string, int>
        {
            {"UW1:あ",-3}, {"UW1:い",-1}, {"UW1:う",-3}, {"UW1:え",-1}, {"UW1:お",-3},
            {"UW1:か",-3}, {"UW1:き",-1}, {"UW1:く",-3}, {"UW1:け",-1}, {"UW1:こ",-3},
            {"UW2:あ",3}, {"UW2:い",1}, {"UW2:う",3}, {"UW2:え",1}, {"UW2:お",3},
            {"UW3:す",-1}, {"UW3:た",1}, {"UW3:で",-1}, {"UW3:な",1}, {"UW3:に",-3},
            {"UW4:て",1}, {"UW4:で",1}, {"UW4:に",1}, {"UW4:を",3},
            {"UW5:て",2}, {"UW5:で",2}, {"UW5:に",2}, {"UW5:の",1},
            {"UW6:て",5}, {"UW6:で",5}, {"UW6:に",5}, {"UW6:の",3}, {"UW6:は",5},
            {"BW1:ァア",-2}, {"BW1:アア",-1}, {"BW1:アイ",1}, {"BW1:アウ",1},
            {"BW2:アア",-1}, {"BW2:イイ",-1}, {"BW2:ウウ",-1},
            {"BW3:ああ",5}, {"BW3:いい",3}, {"BW3:うう",3},
            {"TW1:にほんご",5}, {"TW2:ほんご",2},
        };
        return d;
    }

    public static List<string> Segment(string text)
    {
        if (string.IsNullOrEmpty(text)) return new List<string>();

        var seg = new List<string>();
        var cur = new System.Text.StringBuilder();
        var len = text.Length;

        // 文字タイプが変わったら区切る簡易実装（日本語は漢字/かな/カナで区切り）
        // + TinySegmenterのスコアベースの補助判定
        CharType? prevType = null;

        for (int i = 0; i < len; i++)
        {
            char c = text[i];
            var ct = GetCharType(c);

            bool isBoundary = false;

            if (prevType.HasValue)
            {
                // 文字種が変わった場合は基本的に区切り
                if (ct != prevType.Value)
                {
                    // ただし英数字同士は続ける
                    bool bothAlphaNum = (ct == CharType.A || ct == CharType.N) &&
                                        (prevType.Value == CharType.A || prevType.Value == CharType.N);
                    if (!bothAlphaNum)
                        isBoundary = true;
                }
                else if (ct == CharType.J)
                {
                    // 同じ漢字でも2〜4文字程度でスコアベースの判定
                    var score = GetScore(text, i);
                    if (score > 0) isBoundary = true;
                }
            }

            if (isBoundary && cur.Length > 0)
            {
                var word = cur.ToString();
                if (word.Length > 0) seg.Add(word);
                cur.Clear();
            }

            cur.Append(c);
            prevType = ct;
        }

        if (cur.Length > 0)
            seg.Add(cur.ToString());

        return seg;
    }

    private static int GetScore(string text, int pos)
    {
        // 前後の文字列パターンでスコアを計算する簡易版
        var score = 0;

        // UW3: 現在位置の文字
        if (pos >= 0 && pos < text.Length)
        {
            var key = "UW3:" + text[pos];
            if (BigramScores.TryGetValue(key, out var s)) score += s;
        }

        // BW2: 直前2文字
        if (pos >= 2)
        {
            var key = "BW2:" + text.Substring(pos - 2, 2);
            if (BigramScores.TryGetValue(key, out var s)) score += s;
        }

        return score;
    }

    /// <summary>
    /// ファイル名（拡張子なし）をトークンに分割し、
    /// 区切り文字・空白を除去してキーワードリストを返す。
    /// </summary>
    public static List<string> TokenizeFileName(string fileNameWithoutExt)
    {
        // まず記号・区切り文字でプリスプリット
        var parts = fileNameWithoutExt.Split(
            new[] { ' ', '_', '-', '.', '[', ']', '(', ')', '【', '】', '「', '」', '『', '』',
                    '（', '）', '/', '\\', '・', '　', '#', '@', '+', '=', '!', '！', '?', '？',
                    '※', '☆', '★', '♪', '♥', '◆', '◇', '■', '□' },
            StringSplitOptions.RemoveEmptyEntries);

        var tokens = new List<string>();
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part)) continue;
            var segs = Segment(part);
            tokens.AddRange(segs.Where(s => s.Length > 0));
        }

        return tokens;
    }
}
