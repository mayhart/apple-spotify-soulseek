using Spotify.Slsk.Integration.Models.enums;

namespace Spotify.Slsk.Integration.Tests;

public class MusicalKeyFormatTests
{
    [Theory]
    [InlineData("open-key", "open-key")]
    [InlineData("camelot", "camelot")]
    [InlineData("standard", "standard")]
    [InlineData("unknown", "open-key")] // defaults to open-key
    [InlineData("", "open-key")]
    public void From_ReturnsCorrectFormat(string input, string expectedValue)
    {
        var format = MusicalKeyFormat.from(input);
        Assert.Equal(expectedValue, format.Value);
    }

    [Fact]
    public void OpenKey_HasCorrectValue()
    {
        Assert.Equal("open-key", MusicalKeyFormat.OpenKey.Value);
    }

    [Fact]
    public void Camelot_HasCorrectValue()
    {
        Assert.Equal("camelot", MusicalKeyFormat.Camelot.Value);
    }

    [Fact]
    public void Standard_HasCorrectValue()
    {
        Assert.Equal("standard", MusicalKeyFormat.Standard.Value);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        Assert.Equal("camelot", MusicalKeyFormat.Camelot.ToString());
        Assert.Equal("open-key", MusicalKeyFormat.OpenKey.ToString());
        Assert.Equal("standard", MusicalKeyFormat.Standard.ToString());
    }
}
