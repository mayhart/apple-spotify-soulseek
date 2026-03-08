namespace Spotify.Slsk.Integration.Extensions
{
    public static class ListExtensions
    {
        public static IList<T> Shuffle<T>(this IList<T> list)
        {
            Random rng = new();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[n], list[k]) = (list[k], list[n]);
            }
            return list;
        }
    }
}
