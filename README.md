# apple-spotify-soulseek

A .NET 6 CLI tool that downloads tracks from **Spotify** or **Apple Music** playlists via [Soulseek](https://www.slsknet.org/), searching for high-quality MP3 files automatically.

## Features

- Download all tracks from a Spotify playlist via Soulseek
- Download unsaved Spotify playlist tracks and save them back to your library
- Download a single Spotify track
- Download all tracks from an Apple Music playlist using an exported library XML file (no API credentials needed)
- Optional ID3 tag enrichment for Spotify tracks (BPM, key, etc.)
- Musical key translation (Open Key, Camelot, etc.)

## Requirements

- [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
- A [Soulseek](https://www.slsknet.org/) account
- **Spotify commands:** a Spotify access token (OAuth, obtainable from the [Spotify Developer Console](https://developer.spotify.com/console/))
- **Apple Music command:** an Apple Music library XML file — export from Music.app via **File → Library → Export Library...**

## Build

```bash
cd Source
dotnet build
```

## Usage

```
spotseek <command> [options]
```

### Commands

| Command | Description |
|---|---|
| `download-playlist` | Download all tracks from a Spotify playlist |
| `download-and-save-playlist` | Download unsaved Spotify tracks and save them to your library |
| `download-track` | Download a single Spotify track |
| `download-apple-playlist` | Download all tracks from an Apple Music playlist |
| `translate-key` | Translate a musical key between formats |

Run any command with `--help` for full option details.

---

### download-playlist

Download all tracks from a Spotify playlist.

```bash
spotseek download-playlist \
  -i <spotify-user-id> \
  -l <playlist-id> \
  -a <spotify-access-token> \
  -u <soulseek-username> \
  -p <soulseek-password>
```

| Option | Short | Description |
|---|---|---|
| `--userid` | `-i` | Spotify user ID (not email) |
| `--playlistid` | `-l` | Spotify playlist ID (from share link) |
| `--playlistname` | `-n` | Playlist name (alternative to ID) |
| `--accesstoken` | `-a` | Spotify access token |
| `--ssusername` | `-u` | Soulseek username |
| `--sspassword` | `-p` | Soulseek password |
| `--id3tags` | `-g` | Set ID3 tags after download |
| `--keyformat` | `-k` | Key format for ID3 InitialKey (`openkey`, `camelot`) |
| `--skip-results` | `-s` | Skip tracks already present in the results folder |
| `--flac` | `-f` | Allow FLAC files |
| `--searchtimeout` | `-t` | Max search time per track in seconds (default: 10) |

---

### download-apple-playlist

Download all tracks from an Apple Music playlist using a library XML export.

**Step 1 — export your library:**
In Music.app, go to **File → Library → Export Library...** and save the XML file.

**Step 2 — run the command:**

```bash
spotseek download-apple-playlist \
  -x "/path/to/Music Library.xml" \
  -n "My Playlist" \
  -u <soulseek-username> \
  -p <soulseek-password>
```

| Option | Short | Description |
|---|---|---|
| `--library` | `-x` | Path to the Apple Music library XML file |
| `--playlistid` | `-l` | Playlist Persistent ID from the XML |
| `--playlistname` | `-n` | Playlist name (as shown in Music.app) |
| `--ssusername` | `-u` | Soulseek username |
| `--sspassword` | `-p` | Soulseek password |
| `--skip-results` | `-s` | Skip tracks already present in the results folder |
| `--flac` | `-f` | Allow FLAC files |
| `--searchtimeout` | `-t` | Max search time per track in seconds (default: 10) |

---

### translate-key

Translate a musical key string between notation formats.

```bash
spotseek translate-key --key "1A" --format openkey
```

## Output

Downloaded files are saved to a `Results/<playlist-name>/` folder next to the CLI project. Logs are written to `auto-spoti-logs.log`.
