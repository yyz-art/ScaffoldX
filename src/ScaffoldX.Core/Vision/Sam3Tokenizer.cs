namespace ScaffoldX.Core.Vision;

/// <summary>
/// SAM 3 文本分词器，将文本转换为 Token ID 序列。
/// 使用简化的 BPE 编码，加载 vocab.json 和 merges.txt。
/// </summary>
public class Sam3Tokenizer
{
    private Dictionary<string, int> _vocab = new();
    private List<(string, string)> _merges = new();
    private readonly int _maxLength = 64;

    // 特殊 Token
    private const int BosToken = 49406;
    private const int EosToken = 49407;
    private const int PadToken = 0;

    /// <summary>词表大小。</summary>
    public int VocabSize => _vocab.Count;

    /// <summary>
    /// 从文件加载词表和合并规则。
    /// </summary>
    /// <param name="vocabPath">vocab.json 文件路径。</param>
    /// <param name="mergesPath">merges.txt 文件路径。</param>
    public void LoadVocab(string vocabPath, string mergesPath)
    {
        if (!File.Exists(vocabPath))
            throw new FileNotFoundException("词表文件不存在", vocabPath);
        if (!File.Exists(mergesPath))
            throw new FileNotFoundException("合并规则文件不存在", mergesPath);

        _vocab = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(
            File.ReadAllText(vocabPath)) ?? new Dictionary<string, int>();

        _merges = File.ReadAllLines(mergesPath)
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'))
            .Select(line =>
            {
                var parts = line.Split(' ', 2);
                return parts.Length == 2 ? (parts[0], parts[1]) : ("", "");
            })
            .Where(m => m.Item1 != "" && m.Item2 != "")
            .ToList();
    }

    /// <summary>
    /// 将文本编码为 Token ID 数组。
    /// </summary>
    /// <param name="text">输入文本。</param>
    /// <returns>包含 BOS/EOS 的 Token ID 数组。</returns>
    public int[] Encode(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new[] { BosToken, EosToken };

        var tokens = new List<int> { BosToken };

        var words = text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            var wordTokens = BpeEncode(word);
            tokens.AddRange(wordTokens);
        }

        tokens.Add(EosToken);

        // 截断到最大长度
        if (tokens.Count > _maxLength)
        {
            tokens = tokens.Take(_maxLength - 1).ToList();
            tokens.Add(EosToken);
        }

        return tokens.ToArray();
    }

    /// <summary>
    /// 将文本编码为填充到固定长度的 Token ID 数组。
    /// </summary>
    public int[] EncodePadded(string text, int length = 0)
    {
        var encoded = Encode(text);
        var targetLength = length > 0 ? length : _maxLength;

        if (encoded.Length >= targetLength)
            return encoded.Take(targetLength).ToArray();

        var padded = new int[targetLength];
        Array.Copy(encoded, padded, encoded.Length);
        for (int i = encoded.Length; i < targetLength; i++)
            padded[i] = PadToken;

        return padded;
    }

    /// <summary>
    /// 简化的 BPE 编码。
    /// </summary>
    private List<int> BpeEncode(string word)
    {
        if (_vocab.Count == 0)
        {
            // 无词表时使用字符级编码（ASCII 值作为 token ID）
            return word.Select(c => (int)c % 1000).ToList();
        }

        // 尝试直接查找整个词
        if (_vocab.TryGetValue(word, out var id))
            return new List<int> { id };

        // 字符级拆分
        var chars = word.Select(c => c.ToString()).ToList();
        var tokens = new List<string>(chars);

        // 应用合并规则
        foreach (var (first, second) in _merges)
        {
            var newTokens = new List<string>();
            int i = 0;
            while (i < tokens.Count)
            {
                if (i < tokens.Count - 1 && tokens[i] == first && tokens[i + 1] == second)
                {
                    newTokens.Add(first + second);
                    i += 2;
                }
                else
                {
                    newTokens.Add(tokens[i]);
                    i++;
                }
            }
            tokens = newTokens;
        }

        return tokens
            .Select(t => _vocab.TryGetValue(t, out var tokenId) ? tokenId : PadToken)
            .ToList();
    }

    /// <summary>
    /// 解码 Token ID 数组为文本。
    /// </summary>
    public string Decode(int[] tokenIds)
    {
        if (_vocab.Count == 0)
            return new string(tokenIds.Where(id => id != PadToken && id != BosToken && id != EosToken)
                .Select(id => (char)(id % 1000)).ToArray());

        var reverseVocab = new Dictionary<int, string>();
        foreach (var kv in _vocab)
            reverseVocab.TryAdd(kv.Value, kv.Key);

        var chars = tokenIds
            .Where(id => id != PadToken && id != BosToken && id != EosToken)
            .Select(id => reverseVocab.TryGetValue(id, out var token) ? token : "?");

        return string.Join("", chars);
    }
}
