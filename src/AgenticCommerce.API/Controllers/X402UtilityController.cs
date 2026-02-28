using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AgenticCommerce.Infrastructure.Payments;
using Microsoft.AspNetCore.Mvc;

namespace AgenticCommerce.API.Controllers;

/// <summary>
/// Reference x402 utility APIs — real value behind a paywall.
/// Each endpoint uses [X402Payment] for automatic 402 enforcement.
/// </summary>
[ApiController]
[Route("api/x402/utility")]
public class X402UtilityController : ControllerBase
{
    private const int MaxTextLength = 10_000;
    private const int MaxJsonBodySize = 102_400; // 100KB

    private static readonly HashSet<string> PositiveWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "good", "great", "excellent", "amazing", "wonderful", "fantastic", "awesome",
        "love", "happy", "joy", "beautiful", "brilliant", "perfect", "best", "nice",
        "outstanding", "superb", "incredible", "remarkable", "positive", "impressive",
        "delightful", "exceptional", "magnificent", "pleased", "thrilled", "terrific",
        "fabulous", "marvelous", "splendid", "stellar", "superior", "glorious"
    };

    private static readonly HashSet<string> NegativeWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "bad", "terrible", "awful", "horrible", "worst", "hate", "ugly", "poor",
        "disappointing", "dreadful", "miserable", "disgusting", "pathetic", "lousy",
        "negative", "annoying", "frustrating", "painful", "boring", "useless",
        "inferior", "mediocre", "appalling", "abysmal", "atrocious", "horrendous",
        "wretched", "dismal", "ghastly", "grim", "unpleasant", "vile"
    };

    /// <summary>
    /// Rule-based sentiment analysis — $0.001 per request.
    /// Scores text from -1.0 (negative) to 1.0 (positive).
    /// </summary>
    [HttpGet("sentiment")]
    [X402Payment(0.001, Description = "Sentiment Analysis API")]
    public IActionResult AnalyzeSentiment([FromQuery] string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return BadRequest(new { error = "Query parameter 'text' is required." });
        if (text.Length > MaxTextLength)
            return BadRequest(new { error = $"Text must be {MaxTextLength} characters or fewer." });

        var words = text.Split(new[] { ' ', '\t', '\n', '\r', ',', '.', '!', '?', ';', ':', '"', '\'', '(', ')' },
            StringSplitOptions.RemoveEmptyEntries);
        int pos = 0, neg = 0;
        foreach (var w in words)
        {
            if (PositiveWords.Contains(w)) pos++;
            if (NegativeWords.Contains(w)) neg++;
        }
        int total = words.Length;
        double score = total > 0 ? Math.Clamp((double)(pos - neg) / total, -1.0, 1.0) : 0.0;
        string label = score > 0.05 ? "positive" : score < -0.05 ? "negative" : "neutral";

        return Ok(new
        {
            score = Math.Round(score, 4),
            label,
            positiveWords = pos,
            negativeWords = neg,
            totalWords = total,
            costUsdc = 0.001m,
            payer = HttpContext.GetX402Payer(),
            transactionHash = HttpContext.GetX402TransactionHash()
        });
    }

    /// <summary>
    /// Extractive text summarization — $0.005 per request.
    /// Returns top N sentences ranked by word frequency (TF).
    /// </summary>
    [HttpGet("summarize")]
    [X402Payment(0.005, Description = "Text Summarization API")]
    public IActionResult SummarizeText(
        [FromQuery] string text,
        [FromQuery] int sentences = 3)
    {
        if (string.IsNullOrWhiteSpace(text))
            return BadRequest(new { error = "Query parameter 'text' is required." });
        if (text.Length > MaxTextLength)
            return BadRequest(new { error = $"Text must be {MaxTextLength} characters or fewer." });

        sentences = Math.Clamp(sentences, 1, 20);

        // Split into sentences
        var sentenceList = SplitSentences(text);
        if (sentenceList.Count == 0)
            return Ok(new { summary = "", sentenceCount = 0, costUsdc = 0.005m,
                payer = HttpContext.GetX402Payer(), transactionHash = HttpContext.GetX402TransactionHash() });

        // Compute word frequencies
        var wordFreq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in sentenceList)
        {
            foreach (var w in Tokenize(s))
            {
                wordFreq.TryGetValue(w, out int count);
                wordFreq[w] = count + 1;
            }
        }

        // Score each sentence by sum of word frequencies
        var scored = sentenceList.Select((s, i) => new
        {
            Index = i,
            Text = s.Trim(),
            Score = Tokenize(s).Sum(w => wordFreq.GetValueOrDefault(w, 0))
        }).ToList();

        // Take top N by score, return in original order
        var topIndices = scored
            .OrderByDescending(x => x.Score)
            .Take(sentences)
            .OrderBy(x => x.Index)
            .Select(x => x.Text)
            .ToList();

        return Ok(new
        {
            summary = string.Join(" ", topIndices),
            sentenceCount = topIndices.Count,
            originalSentences = sentenceList.Count,
            costUsdc = 0.005m,
            payer = HttpContext.GetX402Payer(),
            transactionHash = HttpContext.GetX402TransactionHash()
        });
    }

    /// <summary>
    /// JSON transformation — $0.002 per request.
    /// Applies operations: flatten, sort_keys, remove_nulls, count_keys.
    /// </summary>
    [HttpPost("json-transform")]
    [X402Payment(0.002, Description = "JSON Transformation API")]
    public IActionResult TransformJson([FromBody] JsonTransformRequest request)
    {
        if (request.Data == null)
            return BadRequest(new { error = "'data' field is required." });
        if (request.Operations == null || request.Operations.Length == 0)
            return BadRequest(new { error = "'operations' array is required." });

        var validOps = new HashSet<string> { "flatten", "sort_keys", "remove_nulls", "count_keys" };
        var invalid = request.Operations.Where(o => !validOps.Contains(o)).ToArray();
        if (invalid.Length > 0)
            return BadRequest(new { error = $"Invalid operations: {string.Join(", ", invalid)}. Valid: {string.Join(", ", validOps)}" });

        var result = new Dictionary<string, object?>();
        var data = request.Data;

        foreach (var op in request.Operations)
        {
            switch (op)
            {
                case "flatten":
                    var flat = new Dictionary<string, JsonNode?>();
                    FlattenJson(data, "", flat);
                    result["flatten"] = flat.ToDictionary(kv => kv.Key, kv => (object?)kv.Value?.ToJsonString());
                    break;
                case "sort_keys":
                    result["sort_keys"] = SortKeys(data);
                    break;
                case "remove_nulls":
                    result["remove_nulls"] = RemoveNulls(data);
                    break;
                case "count_keys":
                    int count = 0;
                    CountKeys(data, ref count);
                    result["count_keys"] = count;
                    break;
            }
        }

        return Ok(new
        {
            results = result,
            operationsApplied = request.Operations,
            costUsdc = 0.002m,
            payer = HttpContext.GetX402Payer(),
            transactionHash = HttpContext.GetX402TransactionHash()
        });
    }

    /// <summary>
    /// Cryptographic hash — $0.001 per request.
    /// Supports md5, sha1, sha256, sha384, sha512.
    /// </summary>
    [HttpGet("hash")]
    [X402Payment(0.001, Description = "Cryptographic Hash API")]
    public IActionResult HashText(
        [FromQuery] string text,
        [FromQuery] string algorithm = "sha256")
    {
        if (string.IsNullOrWhiteSpace(text))
            return BadRequest(new { error = "Query parameter 'text' is required." });
        if (text.Length > MaxTextLength)
            return BadRequest(new { error = $"Text must be {MaxTextLength} characters or fewer." });

        algorithm = algorithm.ToLowerInvariant();
        byte[] inputBytes = Encoding.UTF8.GetBytes(text);
        byte[] hashBytes;

        try
        {
            hashBytes = algorithm switch
            {
                "md5" => MD5.HashData(inputBytes),
                "sha1" => SHA1.HashData(inputBytes),
                "sha256" => SHA256.HashData(inputBytes),
                "sha384" => SHA384.HashData(inputBytes),
                "sha512" => SHA512.HashData(inputBytes),
                _ => throw new ArgumentException($"Unsupported algorithm: {algorithm}. Use md5, sha1, sha256, sha384, or sha512.")
            };
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        return Ok(new
        {
            hash = Convert.ToHexString(hashBytes).ToLowerInvariant(),
            algorithm,
            inputLength = text.Length,
            costUsdc = 0.001m,
            payer = HttpContext.GetX402Payer(),
            transactionHash = HttpContext.GetX402TransactionHash()
        });
    }

    #region Helpers

    private static List<string> SplitSentences(string text)
    {
        var sentences = new List<string>();
        int start = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] is '.' or '!' or '?')
            {
                // Look ahead to avoid splitting on abbreviations like "e.g."
                if (i + 1 < text.Length && char.IsWhiteSpace(text[i + 1]) || i + 1 == text.Length)
                {
                    var s = text[start..(i + 1)].Trim();
                    if (s.Length > 0) sentences.Add(s);
                    start = i + 1;
                }
            }
        }
        // Remaining text without terminal punctuation
        if (start < text.Length)
        {
            var s = text[start..].Trim();
            if (s.Length > 0) sentences.Add(s);
        }
        return sentences;
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        return text.Split(new[] { ' ', '\t', '\n', '\r', ',', '.', '!', '?', ';', ':', '"', '\'', '(', ')' },
            StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.ToLowerInvariant())
            .Where(w => w.Length > 2); // Skip very short words
    }

    private static void FlattenJson(JsonNode? node, string prefix, Dictionary<string, JsonNode?> result)
    {
        if (node is JsonObject obj)
        {
            foreach (var prop in obj)
            {
                var key = string.IsNullOrEmpty(prefix) ? prop.Key : $"{prefix}.{prop.Key}";
                FlattenJson(prop.Value, key, result);
            }
        }
        else if (node is JsonArray arr)
        {
            for (int i = 0; i < arr.Count; i++)
            {
                var key = $"{prefix}[{i}]";
                FlattenJson(arr[i], key, result);
            }
        }
        else
        {
            result[prefix] = node?.DeepClone();
        }
    }

    private static object? SortKeys(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            var sorted = new SortedDictionary<string, object?>();
            foreach (var prop in obj)
                sorted[prop.Key] = SortKeys(prop.Value);
            return sorted;
        }
        if (node is JsonArray arr)
            return arr.Select(SortKeys).ToList();
        return node?.ToJsonString();
    }

    private static object? RemoveNulls(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            var clean = new Dictionary<string, object?>();
            foreach (var prop in obj)
            {
                if (prop.Value != null)
                    clean[prop.Key] = RemoveNulls(prop.Value);
            }
            return clean;
        }
        if (node is JsonArray arr)
            return arr.Where(n => n != null).Select(RemoveNulls).ToList();
        return node?.ToJsonString();
    }

    private static void CountKeys(JsonNode? node, ref int count)
    {
        if (node is JsonObject obj)
        {
            count += obj.Count;
            foreach (var prop in obj)
                CountKeys(prop.Value, ref count);
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
                CountKeys(item, ref count);
        }
    }

    #endregion
}

public class JsonTransformRequest
{
    public JsonNode? Data { get; set; }
    public string[] Operations { get; set; } = Array.Empty<string>();
}
