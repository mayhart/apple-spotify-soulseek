using Spotify.Slsk.Integration.Extensions;

namespace Spotify.Slsk.Integration.Tests;

public class ListExtensionsTests
{
    [Fact]
    public void Shuffle_ReturnsSameElements()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        var original = list.ToList();

        list.Shuffle();

        Assert.Equal(original.Count, list.Count);
        foreach (var item in original)
        {
            Assert.Contains(item, list);
        }
    }

    [Fact]
    public void Shuffle_ReturnsTheSameList()
    {
        var list = new List<int> { 1, 2, 3 };
        var result = list.Shuffle();
        Assert.Same(list, result);
    }

    [Fact]
    public void Shuffle_EmptyList_DoesNotThrow()
    {
        var list = new List<int>();
        var result = list.Shuffle();
        Assert.Empty(result);
    }

    [Fact]
    public void Shuffle_SingleElement_ReturnsSameElement()
    {
        var list = new List<string> { "only" };
        list.Shuffle();
        Assert.Single(list);
        Assert.Equal("only", list[0]);
    }
}
