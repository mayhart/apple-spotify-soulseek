using Spotify.Slsk.Integration.Models.AppleMusic;
using System.Xml;

namespace Spotify.Slsk.Integration.Clients.AppleMusic
{
    /// <summary>
    /// Parses an Apple Music / iTunes library XML file exported via
    /// Music.app → File → Library → Export Library...
    /// (or enabled automatically at ~/Music/Music/Music Library.xml)
    /// </summary>
    public class AppleMusicLibraryClient
    {
        private readonly XmlDocument _doc;

        public AppleMusicLibraryClient(string libraryXmlPath)
        {
            if (!File.Exists(libraryXmlPath))
            {
                throw new FileNotFoundException($"Apple Music library XML file not found: '{libraryXmlPath}'");
            }

            _doc = new XmlDocument();
            _doc.Load(libraryXmlPath);
        }

        /// <summary>
        /// Returns all playlists defined in the library XML.
        /// </summary>
        public List<AppleMusicPlaylist> GetAllPlaylists()
        {
            Dictionary<int, AppleMusicTrack> tracksById = ParseTracks();

            XmlNode? playlistsArray = _doc.SelectSingleNode("/plist/dict/key[.='Playlists']/following-sibling::array[1]");
            if (playlistsArray == null)
            {
                return new List<AppleMusicPlaylist>();
            }

            List<AppleMusicPlaylist> result = new();
            foreach (XmlNode playlistDict in playlistsArray.ChildNodes)
            {
                Dictionary<string, XmlNode> fields = ParseDict(playlistDict);

                if (!fields.TryGetValue("Name", out XmlNode? nameNode))
                {
                    continue;
                }

                string name = nameNode.InnerText;
                string playlistId = fields.TryGetValue("Playlist Persistent ID", out XmlNode? idNode)
                    ? idNode.InnerText
                    : name;

                List<AppleMusicTrack> tracks = new();
                if (fields.TryGetValue("Playlist Items", out XmlNode? itemsNode))
                {
                    foreach (XmlNode itemDict in itemsNode.ChildNodes)
                    {
                        Dictionary<string, XmlNode> itemFields = ParseDict(itemDict);
                        if (itemFields.TryGetValue("Track ID", out XmlNode? trackIdNode)
                            && int.TryParse(trackIdNode.InnerText, out int trackId)
                            && tracksById.TryGetValue(trackId, out AppleMusicTrack? track))
                        {
                            tracks.Add(track);
                        }
                    }
                }

                result.Add(new AppleMusicPlaylist
                {
                    Id = playlistId,
                    Name = name,
                    Tracks = tracks
                });
            }

            return result;
        }

        /// <summary>
        /// Returns a playlist by name (case-sensitive).
        /// </summary>
        public AppleMusicPlaylist GetPlaylistByName(string playlistName)
        {
            AppleMusicPlaylist? playlist = GetAllPlaylists().FirstOrDefault(p => p.Name == playlistName);
            return playlist
                ?? throw new Exception($"Playlist '{playlistName}' not found in the library XML");
        }

        /// <summary>
        /// Returns a playlist by its Persistent ID.
        /// </summary>
        public AppleMusicPlaylist GetPlaylistById(string playlistId)
        {
            AppleMusicPlaylist? playlist = GetAllPlaylists().FirstOrDefault(p => p.Id == playlistId);
            return playlist
                ?? throw new Exception($"Playlist with ID '{playlistId}' not found in the library XML");
        }

        private Dictionary<int, AppleMusicTrack> ParseTracks()
        {
            Dictionary<int, AppleMusicTrack> result = new();

            XmlNode? tracksDict = _doc.SelectSingleNode("/plist/dict/key[.='Tracks']/following-sibling::dict[1]");
            if (tracksDict == null)
            {
                return result;
            }

            XmlNodeList children = tracksDict.ChildNodes;
            for (int i = 0; i + 1 < children.Count; i += 2)
            {
                XmlNode keyNode = children[i]!;
                XmlNode dictNode = children[i + 1]!;

                if (!int.TryParse(keyNode.InnerText, out int trackId))
                {
                    continue;
                }

                Dictionary<string, XmlNode> fields = ParseDict(dictNode);

                result[trackId] = new AppleMusicTrack
                {
                    Id = trackId.ToString(),
                    Name = fields.TryGetValue("Name", out XmlNode? n) ? n.InnerText : string.Empty,
                    Artist = fields.TryGetValue("Artist", out XmlNode? a) ? a.InnerText : string.Empty,
                    Album = fields.TryGetValue("Album", out XmlNode? al) ? al.InnerText : string.Empty,
                    DurationMs = fields.TryGetValue("Total Time", out XmlNode? tt) && int.TryParse(tt.InnerText, out int ms) ? ms : null
                };
            }

            return result;
        }

        private static Dictionary<string, XmlNode> ParseDict(XmlNode dictNode)
        {
            Dictionary<string, XmlNode> result = new();
            XmlNodeList children = dictNode.ChildNodes;
            for (int i = 0; i + 1 < children.Count; i += 2)
            {
                string key = children[i]!.InnerText;
                result[key] = children[i + 1]!;
            }
            return result;
        }
    }
}
