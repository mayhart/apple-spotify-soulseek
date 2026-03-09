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
    public void RemoveSpecialCharacters_ReturnsOnlyAllowedChars(string input, string expected)
    {
        var result = input.RemoveSpecialCharacters();
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
