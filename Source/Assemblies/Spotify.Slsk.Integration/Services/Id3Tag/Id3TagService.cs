using Spotify.Slsk.Integration.Models.enums;
using Spotify.Slsk.Integration.Models.Spotify;
using Spotify.Slsk.Integration.Services.Id3Tag.Translators;
using Serilog;
using TagLib;

namespace Spotify.Slsk.Integration.Services.Id3Tag
{
    public static class Id3TagService
    {
        /// <summary>
        /// Sets ID3 tags on the downloaded file using Spotify metadata and audio features.
        /// </summary>
        public static void SetId3Tags(TrackItem trackItem, AudioFeatures audioFeatures, MusicalKeyFormat musicalKeyFormat, string filePath)
        {
            try
            {
                using TagLib.File file = TagLib.File.Create(filePath);

                Track track = trackItem.Track!;
                file.Tag.Title = track.Name;
                file.Tag.Performers = track.Artists?.Select(a => a.Name).ToArray()!;
                file.Tag.Album = track.Album?.Name;
                file.Tag.BeatsPerMinute = (uint)(audioFeatures.Tempo ?? 0);

                if (audioFeatures.Key.HasValue && audioFeatures.Mode.HasValue)
                {
                    string translatedKey = MusicalKeyTranslator.Translate(audioFeatures.Key.Value, audioFeatures.Mode.Value, musicalKeyFormat);
                    file.Tag.InitialKey = translatedKey;
                }

                file.Save();
                Log.Information($"ID3 tags set for '{filePath}'.");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to set ID3 tags for '{filePath}': {ex.Message}");
            }
        }

        /// <summary>
        /// Translates the InitialKey ID3 tag for all mp3 files in the given folder to Open Key format.
        /// </summary>
        public static void TranslateMusicalKeyForFilesInFolder(string folderPath)
        {
            string[] files = Directory.GetFiles(folderPath, "*.mp3", SearchOption.AllDirectories);
            Log.Information($"Found {files.Length} mp3 file(s) in '{folderPath}'.");

            foreach (string filePath in files)
            {
                try
                {
                    using TagLib.File file = TagLib.File.Create(filePath);
                    string? currentKey = file.Tag.InitialKey;
                    if (string.IsNullOrEmpty(currentKey))
                    {
                        Log.Warning($"No InitialKey tag found for '{filePath}', skipping.");
                        continue;
                    }

                    string openKey = MusicalKeyTranslator.CamelotToOpenKey(currentKey);
                    if (openKey != currentKey)
                    {
                        file.Tag.InitialKey = openKey;
                        file.Save();
                        Log.Information($"Translated key '{currentKey}' -> '{openKey}' for '{filePath}'.");
                    }
                    else
                    {
                        Log.Information($"Key '{currentKey}' already in Open Key format for '{filePath}', skipping.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to translate key for '{filePath}': {ex.Message}");
                }
            }
        }
    }
}
