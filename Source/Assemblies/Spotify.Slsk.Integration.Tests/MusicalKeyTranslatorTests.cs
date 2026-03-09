using Spotify.Slsk.Integration.Models.enums;
using Spotify.Slsk.Integration.Services.Id3Tag.Translators;

namespace Spotify.Slsk.Integration.Tests;

public class MusicalKeyTranslatorTests
{
    [Theory]
    [InlineData(0, 1, "8B")]   // C major
    [InlineData(0, 0, "8A")]   // A minor
    [InlineData(7, 1, "9B")]   // G major
    [InlineData(7, 0, "9A")]   // E minor
    [InlineData(11, 1, "1B")]  // B major
    [InlineData(11, 0, "1A")]  // G# minor
    public void Translate_Camelot_ReturnsExpected(int key, int mode, string expected)
    {
        var result = MusicalKeyTranslator.Translate(key, mode, MusicalKeyFormat.Camelot);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, 1, "8d")]   // C major
    [InlineData(0, 0, "8m")]   // A minor
    [InlineData(7, 1, "9d")]   // G major
    [InlineData(7, 0, "9m")]   // E minor
    [InlineData(9, 1, "11d")]  // A major
    [InlineData(9, 0, "11m")]  // F# minor
    public void Translate_OpenKey_ReturnsExpected(int key, int mode, string expected)
    {
        var result = MusicalKeyTranslator.Translate(key, mode, MusicalKeyFormat.OpenKey);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, 1, "C")]    // C major
    [InlineData(0, 0, "Am")]   // A minor
    [InlineData(2, 1, "D")]    // D major
    [InlineData(2, 0, "Bm")]   // B minor
    [InlineData(5, 1, "F")]    // F major
    [InlineData(5, 0, "Dm")]   // D minor
    [InlineData(9, 1, "A")]    // A major
    [InlineData(9, 0, "F#m")]  // F# minor
    public void Translate_Standard_ReturnsExpected(int key, int mode, string expected)
    {
        var result = MusicalKeyTranslator.Translate(key, mode, MusicalKeyFormat.Standard);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(12, 0)]
    [InlineData(-100, 1)]
    public void Translate_InvalidKey_ReturnsEmpty(int key, int mode)
    {
        var result = MusicalKeyTranslator.Translate(key, mode, MusicalKeyFormat.Standard);
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("8B", "8d")]   // C major
    [InlineData("8A", "8m")]   // A minor
    [InlineData("9B", "9d")]   // G major
    [InlineData("9A", "9m")]   // E minor
    [InlineData("1B", "1d")]   // B major
    [InlineData("1A", "1m")]   // G# minor
    public void CamelotToOpenKey_ReturnsExpected(string camelot, string expected)
    {
        var result = MusicalKeyTranslator.CamelotToOpenKey(camelot);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CamelotToOpenKey_UnknownKey_ReturnsInput()
    {
        var result = MusicalKeyTranslator.CamelotToOpenKey("999X");
        Assert.Equal("999X", result);
    }
}
