using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Spotify.Slsk.Integration.Extensions
{
    public static class StringExtensions
    {
        public static string RemoveSpecialCharacters(this string str)
        {
            string normalized = str.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new();
            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == ' ' || c == '-' || c == '_')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static string NormalizeForSearch(this string str)
        {
            // Remove parenthetical/bracketed content — feat. tags, remix info, radio edits, etc.
            str = Regex.Replace(str, @"\(.*?\)", " ");
            str = Regex.Replace(str, @"\[.*?\]", " ");

            // Remove featured artist mentions that appear outside brackets
            str = Regex.Replace(str, @"\s*(feat\.?|ft\.?|featuring)\b.*$", " ", RegexOptions.IgnoreCase);

            // Replace & with "and" so it survives ASCII stripping
            str = str.Replace("&", " and ");

            // Replace en/em dashes with spaces to avoid merging words
            str = str.Replace("–", " ").Replace("—", " ");

            // Strip diacritics and non-ASCII special characters
            str = str.RemoveSpecialCharacters();

            // Collapse runs of whitespace
            return Regex.Replace(str, @"\s+", " ").Trim();
        }

        public static string ToLocalOSPath(this string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return path.Replace("/", @"\");
            }
            else
            {
                return path.Replace(@"\", "/");
            }
        }

        public static string ToMB(this long bytes)
        {
            return $"{bytes / 1024.0 / 1024.0:N2}MB";
        }

        public static string ToMB(this double bytesPerSecond)
        {
            return $"{bytesPerSecond / 1024.0 / 1024.0:N2}MB";
        }
    }
}
