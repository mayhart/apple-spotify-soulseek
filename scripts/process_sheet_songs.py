#!/usr/bin/env python3
"""
Reads a local CSV for new song entries, downloads each via the Spotseek CLI,
and writes the result status back to the CSV.

CSV columns:
  Track, Artist, Date Added, Status, Processed At, URL, Vibe

Required environment variables:
  SOULSEEK_USERNAME    – Soulseek login
  SOULSEEK_PASSWORD    – Soulseek password

Optional:
  SONGS_FILE   – path to the CSV
                 (default: ~/Library/Mobile Documents/com~apple~CloudDocs/spotseek-songs.csv)
  CLI_PATH     – path to Spotify.Slsk.Integration.Cli binary (auto-detected by platform if unset)
  CLI_TIMEOUT  – seconds to wait per download (default: 120)
  DRY_RUN      – set to "1" to print what would run without calling the CLI
"""

import csv
import os
import platform
import re
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

# ---------------------------------------------------------------------------
# Config
# ---------------------------------------------------------------------------

SOULSEEK_USERNAME = os.environ["SOULSEEK_USERNAME"]
SOULSEEK_PASSWORD = os.environ["SOULSEEK_PASSWORD"]

_icloud = Path.home() / "Library/Mobile Documents/com~apple~CloudDocs"
_default_songs_file = _icloud / "spotseek-songs.csv"
SONGS_FILE = Path(os.environ.get("SONGS_FILE", str(_default_songs_file)))

_repo_root = Path(__file__).resolve().parent.parent


def _default_cli_path() -> Path:
    system, machine = platform.system(), platform.machine()
    if system == "Darwin" and machine == "arm64":
        rid = "osx-arm64"
    elif system == "Darwin":
        rid = "osx-x64"
    elif machine == "aarch64":
        rid = "linux-arm64"
    else:
        rid = "linux-x64"
    return _repo_root / "dist" / "cli" / rid / "Spotify.Slsk.Integration.Cli"


CLI_PATH = Path(os.environ.get("CLI_PATH", str(_default_cli_path())))
CLI_TIMEOUT = int(os.environ.get("CLI_TIMEOUT", "120"))
DRY_RUN = os.environ.get("DRY_RUN", "0") == "1"

COL_TRACK = 0
COL_ARTIST = 1
COL_DATE_ADDED = 2
COL_STATUS = 3
COL_PROCESSED_AT = 4
COL_URL = 5
COL_VIBE = 6

STATUS_NEW = "New"
STATUS_PROCESSING = "Processing"
STATUS_DOWNLOADED = "Downloaded"
STATUS_NOT_FOUND = "Not Found"

_APPLE_MUSIC_RE = re.compile(r"^(.+?)\s+by\s+(.+?)\s+on\s+Apple\s+Music$", re.IGNORECASE)

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def cell(row: list[str], col: int) -> str:
    return row[col].strip() if len(row) > col else ""


def build_query(row: list[str]) -> str:
    track_col = cell(row, COL_TRACK)
    artist_col = cell(row, COL_ARTIST)
    match = _APPLE_MUSIC_RE.match(track_col)
    if match:
        return f"{match.group(2).strip()} {match.group(1).strip()}"
    if artist_col:
        return f"{artist_col} {track_col}"
    return track_col


def human_label(row: list[str]) -> str:
    track_col = cell(row, COL_TRACK)
    match = _APPLE_MUSIC_RE.match(track_col)
    if match:
        return f"{match.group(2).strip()} - {match.group(1).strip()}"
    artist_col = cell(row, COL_ARTIST)
    return f"{artist_col} - {track_col}" if artist_col else track_col


def run_cli(query: str) -> tuple[bool, str]:
    if DRY_RUN:
        print(f"  [DRY RUN] would run: {CLI_PATH} download-track --query {query!r}")
        return True, ""
    cmd = [str(CLI_PATH), "download-track", "--query", query]
    env = {**os.environ, "SOULSEEK_USERNAME": SOULSEEK_USERNAME, "SOULSEEK_PASSWORD": SOULSEEK_PASSWORD}
    try:
        result = subprocess.run(cmd, env=env, timeout=CLI_TIMEOUT, capture_output=True, text=True)
    except subprocess.TimeoutExpired:
        return False, f"Timed out after {CLI_TIMEOUT}s"
    except Exception as exc:
        return False, str(exc)
    if result.returncode == 0:
        return True, ""
    stderr_lines = result.stderr.strip().splitlines()
    return False, stderr_lines[-1] if stderr_lines else f"exit {result.returncode}"


def now_utc() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC")


def _write_csv(header: list[str], data_rows: list[list[str]]) -> None:
    tmp = SONGS_FILE.with_suffix(".tmp")
    with open(tmp, "w", newline="") as f:
        writer = csv.writer(f)
        writer.writerow(header)
        writer.writerows(data_rows)
    tmp.replace(SONGS_FILE)


def ensure_csv_exists() -> None:
    if not SONGS_FILE.exists():
        SONGS_FILE.parent.mkdir(parents=True, exist_ok=True)
        with open(SONGS_FILE, "w", newline="") as f:
            csv.writer(f).writerow(["Track", "Artist", "Date Added", "Status", "Processed At", "URL", "Vibe"])
        print(f"Created new songs file: {SONGS_FILE}")


# ---------------------------------------------------------------------------
# Main processing loop
# ---------------------------------------------------------------------------


def process() -> None:
    ensure_csv_exists()

    with open(SONGS_FILE, newline="") as f:
        all_rows = list(csv.reader(f))

    if not all_rows:
        print("CSV is empty.")
        return

    header, data_rows = all_rows[0], all_rows[1:]
    new_indices = [i for i, row in enumerate(data_rows) if cell(row, COL_STATUS) == STATUS_NEW]

    if not new_indices:
        print("No new songs to process.")
        return

    print(f"Found {len(new_indices)} new song(s) to process.")

    for i in new_indices:
        row = data_rows[i]
        while len(row) <= COL_PROCESSED_AT:
            row.append("")

        query = build_query(row)
        label = human_label(row)

        if not query:
            print(f"  Row {i + 2}: skipping — track column is empty.")
            row[COL_STATUS] = "Skipped"
            row[COL_PROCESSED_AT] = now_utc()
            _write_csv(header, data_rows)
            continue

        print(f"  Row {i + 2}: '{label}' → query: {query!r}")
        row[COL_STATUS] = STATUS_PROCESSING
        _write_csv(header, data_rows)

        success, note = run_cli(query)
        row[COL_STATUS] = STATUS_DOWNLOADED if success else STATUS_NOT_FOUND
        row[COL_PROCESSED_AT] = now_utc()
        _write_csv(header, data_rows)

        if success:
            print(f"    ✓ Downloaded")
        else:
            print(f"    ✗ Not Found: {note}")


def main() -> None:
    missing = [v for v in ("SOULSEEK_USERNAME", "SOULSEEK_PASSWORD") if not os.environ.get(v)]
    if missing:
        print(f"Error: missing env vars: {', '.join(missing)}", file=sys.stderr)
        sys.exit(1)

    if not DRY_RUN and not CLI_PATH.exists():
        print(f"Error: CLI binary not found at {CLI_PATH}", file=sys.stderr)
        print("Run scripts/publish-cli.sh first, or set CLI_PATH.", file=sys.stderr)
        sys.exit(1)

    print(f"[{now_utc()}] Starting song processor (dry_run={DRY_RUN})")
    print(f"Songs file: {SONGS_FILE}")
    process()
    print(f"[{now_utc()}] Done.")


if __name__ == "__main__":
    main()
