using FluentAssertions;
using ScaffoldX.Core.Vision;
using Xunit;

namespace ScaffoldX.Core.Tests.Vision;

/// <summary>
/// Unit tests for <see cref="Sam3Tokenizer"/> covering encoding, decoding,
/// padding, truncation, and fallback character-level behavior.
/// </summary>
public class Sam3TokenizerTests
{
    private const int BosToken = 49406;
    private const int EosToken = 49407;
    private const int PadToken = 0;

    private readonly Sam3Tokenizer _tokenizer = new();

    // ── Initial state ───────────────────────────────────────────────────────

    [Fact]
    public void VocabSize_InitiallyZero()
    {
        _tokenizer.VocabSize.Should().Be(0);
    }

    // ── Encode ──────────────────────────────────────────────────────────────

    [Fact]
    public void Encode_EmptyString_ReturnsBosEosOnly()
    {
        var result = _tokenizer.Encode("");

        result.Should().Equal(BosToken, EosToken);
    }

    [Fact]
    public void Encode_WhitespaceOnly_ReturnsBosEosOnly()
    {
        var result = _tokenizer.Encode("   ");

        result.Should().Equal(BosToken, EosToken);
    }

    [Fact]
    public void Encode_NullText_ReturnsBosEosOnly()
    {
        var result = _tokenizer.Encode(null!);

        result.Should().Equal(BosToken, EosToken);
    }

    [Fact]
    public void Encode_SingleWord_ContainsBosAndEos()
    {
        // Without vocab, character-level encoding is used: each char → (int)c % 1000
        var result = _tokenizer.Encode("a");

        result.First().Should().Be(BosToken);
        result.Last().Should().Be(EosToken);
    }

    [Fact]
    public void Encode_MultipleWords_SplitsBySpace()
    {
        // "a b" should produce BOS + tokens for 'a' + tokens for 'b' + EOS
        var result = _tokenizer.Encode("a b");

        result.First().Should().Be(BosToken);
        result.Last().Should().Be(EosToken);
        // Should have more tokens than just BOS + single char + EOS
        result.Length.Should().BeGreaterThan(3);
    }

    [Fact]
    public void Encode_WithoutVocab_UsesCharacterLevelEncoding()
    {
        // Character 'a' = 97, so token should be 97 % 1000 = 97
        var result = _tokenizer.Encode("a");

        // BOS, char token, EOS
        result.Should().HaveCount(3);
        result[1].Should().Be(97 % 1000);
    }

    [Fact]
    public void Encode_ConvertToLowercase()
    {
        // 'A' (65) and 'a' (97) should produce same result after ToLowerInvariant
        var upper = _tokenizer.Encode("A");
        var lower = _tokenizer.Encode("a");

        upper.Should().Equal(lower);
    }

    [Fact]
    public void Encode_LongText_TruncatesToMaxLength()
    {
        // Default maxLength is 64; create a string with many characters
        var longText = new string('x', 200);
        var result = _tokenizer.Encode(longText);

        result.Length.Should().BeLessThanOrEqualTo(64);
        result.Last().Should().Be(EosToken); // last token should be EOS after truncation
    }

    // ── EncodePadded ────────────────────────────────────────────────────────

    [Fact]
    public void EncodePadded_ShortText_PadsWithZero()
    {
        var result = _tokenizer.EncodePadded("a", 10);

        result.Should().HaveCount(10);
        // First token is BOS
        result[0].Should().Be(BosToken);
        // Last padded positions should be PAD (0)
        result[9].Should().Be(PadToken);
    }

    [Fact]
    public void EncodePadded_ExactLength_NoPadding()
    {
        // "a" → BOS + char + EOS = 3 tokens, request exactly 3
        var result = _tokenizer.EncodePadded("a", 3);

        result.Should().HaveCount(3);
    }

    [Fact]
    public void EncodePadded_ZeroLength_UsesDefaultMaxLength()
    {
        var result = _tokenizer.EncodePadded("a", 0);

        result.Should().HaveCount(64); // default _maxLength
    }

    [Fact]
    public void EncodePadded_TextLongerThanLength_Truncates()
    {
        var longText = new string('y', 100);
        var result = _tokenizer.EncodePadded(longText, 10);

        result.Should().HaveCount(10);
    }

    // ── Decode ──────────────────────────────────────────────────────────────

    [Fact]
    public void Decode_WithoutVocab_ConvertsTokenIdsToChars()
    {
        // Without vocab, Decode converts id → (char)(id % 1000)
        // 'a' = 97, so token 97 should decode back to 'a'
        var tokenIds = new[] { BosToken, 97, EosToken };

        var result = _tokenizer.Decode(tokenIds);

        result.Should().Be("a");
    }

    [Fact]
    public void Decode_FiltersSpecialTokens()
    {
        // PAD, BOS, EOS should be filtered out
        var tokenIds = new[] { PadToken, BosToken, 65, EosToken, PadToken };

        var result = _tokenizer.Decode(tokenIds);

        // 65 % 1000 = 65 = 'A'
        result.Should().Be("A");
    }

    [Fact]
    public void Decode_EmptyTokenIds_ReturnsEmptyString()
    {
        var result = _tokenizer.Decode(new[] { BosToken, EosToken });

        result.Should().BeEmpty();
    }

    // ── Round-trip ──────────────────────────────────────────────────────────

    [Fact]
    public void EncodeDecode_RoundTrip_PreservesText()
    {
        var original = "hello";

        var encoded = _tokenizer.Encode(original);
        var decoded = _tokenizer.Decode(encoded);

        decoded.Should().Be(original);
    }
}
