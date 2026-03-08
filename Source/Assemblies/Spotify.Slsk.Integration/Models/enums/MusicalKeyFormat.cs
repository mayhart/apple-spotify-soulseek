namespace Spotify.Slsk.Integration.Models.enums
{
    public class MusicalKeyFormat
    {
        public static readonly MusicalKeyFormat OpenKey = new("open-key");
        public static readonly MusicalKeyFormat Camelot = new("camelot");
        public static readonly MusicalKeyFormat Standard = new("standard");

        public string Value { get; }

        private MusicalKeyFormat(string value)
        {
            Value = value;
        }

        public static MusicalKeyFormat from(string value)
        {
            return value switch
            {
                "open-key" => OpenKey,
                "camelot" => Camelot,
                "standard" => Standard,
                _ => OpenKey
            };
        }

        public override string ToString() => Value;
    }
}
