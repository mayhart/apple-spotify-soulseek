using Spotify.Slsk.Integration.Models.enums;

namespace Spotify.Slsk.Integration.Services.Id3Tag.Translators
{
    /// <summary>
    /// Translates Spotify audio feature key/mode values into musical key notation strings.
    /// Spotify returns key as a Pitch Class integer (0=C, 1=C#/Db, ..., 11=B) and mode (0=minor, 1=major).
    /// </summary>
    public static class MusicalKeyTranslator
    {
        // Pitch class index -> (Camelot, OpenKey, Standard Major, Standard Minor)
        private static readonly (string Camelot, string OpenKey, string StandardMajor, string StandardMinor)[] KeyTable = new[]
        {
            ("8B",  "8d", "C",   "Am"),   // 0
            ("3B",  "3d", "Db",  "Bbm"),  // 1
            ("10B", "10d","D",   "Bm"),   // 2
            ("5B",  "5d", "Eb",  "Cm"),   // 3
            ("12B", "12d","E",   "C#m"),  // 4
            ("7B",  "7d", "F",   "Dm"),   // 5
            ("2B",  "2d", "F#",  "Ebm"),  // 6
            ("9B",  "9d", "G",   "Em"),   // 7
            ("4B",  "4d", "Ab",  "Fm"),   // 8
            ("11B", "11d","A",   "F#m"),  // 9
            ("6B",  "6d", "Bb",  "Gm"),   // 10
            ("1B",  "1d", "B",   "G#m"),  // 11
        };

        public static string Translate(int key, int mode, MusicalKeyFormat format)
        {
            if (key < 0 || key > 11)
            {
                return string.Empty;
            }

            var row = KeyTable[key];
            bool isMajor = mode == 1;

            return format.Value switch
            {
                "camelot" => isMajor ? row.Camelot : row.Camelot.Replace("B", "A"),
                "open-key" => isMajor ? row.OpenKey : row.OpenKey.Replace("d", "m"),
                "standard" => isMajor ? row.StandardMajor : row.StandardMinor,
                _ => isMajor ? row.OpenKey : row.OpenKey.Replace("d", "m"),
            };
        }

        /// <summary>
        /// Converts a Camelot key string to Open Key format.
        /// </summary>
        public static string CamelotToOpenKey(string camelotKey)
        {
            foreach (var row in KeyTable)
            {
                if (row.Camelot == camelotKey)
                {
                    return row.OpenKey;
                }
                string camelotMinor = camelotKey.Replace("B", "A");
                if (row.Camelot.Replace("B", "A") == camelotMinor)
                {
                    return row.OpenKey.Replace("d", "m");
                }
            }
            return camelotKey;
        }
    }
}
