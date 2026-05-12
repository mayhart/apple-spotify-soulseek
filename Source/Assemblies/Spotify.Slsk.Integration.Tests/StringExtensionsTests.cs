using Spotify.Slsk.Integration.Extensions;

namespace Spotify.Slsk.Integration.Tests;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("Hello World!", "Hello World")]
    [InlineData("foo@bar.com", "foobarcom")]
    [InlineData("test-track_01", "test-track_01")]
    [InlineData("", "")]
    [InlineData("abc 123", "abc 123")]
    [InlineData("!@#$%^&*()", "")]
    [InlineData("can't", "can't")]
    [InlineData("it's alright", "it's alright")]
    public void RemoveSpecialCharacters_ReturnsOnlyAllowedChars(string input, string expected)
    {
        var result = input.RemoveSpecialCharacters();
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Can't Stop the Feeling Justin Timberlake", "Can't Stop the Feeling Justin Timberlake")]
    [InlineData("Song (feat. Artist2)", "Song Artist2")]
    [InlineData("Song [feat. Artist2]", "Song Artist2")]
    [InlineData("Song feat. Artist2", "Song Artist2")]
    [InlineData("Song ft. Artist2", "Song Artist2")]
    [InlineData("Song featuring Artist2", "Song Artist2")]
    [InlineData("Song (Remastered 2011)", "Song Remastered 2011")]
    [InlineData("Café (feat. Artíst)", "Cafe Artist")]
    public void NormalizeForSearch_HandlesContrationsAndFeatures(string input, string expected)
    {
        var result = input.NormalizeForSearch();
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1024L * 1024L, "1.00MB")]
    [InlineData(0L, "0.00MB")]
    [InlineData(1024L * 1024L * 2, "2.00MB")]
    public void ToMB_Long_ReturnsFormattedString(long bytes, string expected)
    {
        var result = bytes.ToMB();
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1024.0 * 1024.0, "1.00MB")]
    [InlineData(0.0, "0.00MB")]
    [InlineData(1024.0 * 1024.0 * 2.5, "2.50MB")]
    public void ToMB_Double_ReturnsFormattedString(double bytes, string expected)
    {
        var result = bytes.ToMB();
        Assert.Equal(expected, result);
    }
}
