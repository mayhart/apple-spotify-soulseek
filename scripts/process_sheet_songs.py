#!/usr/bin/env python3
"""
Reads a Google Sheet for new song entries, downloads each via the Spotseek CLI,
and writes the result status back to the sheet.

Sheet columns (1-indexed):
  A  Track         e.g. "La Palma by Pablo Fierro on Apple Music" or just "La Niña Grande"
  B  Artist        e.g. "Thornato & Soto" (may be empty if encoded in column A)
  C  Date Added
  D  Status        "New" = needs processing; anything else = skip
  E  Processed At  written after each run
  F  URL           Apple Music link (for reference only)
  G  Vibe          untouched

Required environment variables:
  GOOGLE_SHEET_ID      – the ID from the sheet URL
  GOOGLE_CREDS_PATH    – path to a service-account JSON credentials file
  SOULSEEK_USERNAME    – Soulseek login (passed to CLI)
  SOULSEEK_PASSWORD    – Soulseek password (passed to CLI)

Optional:
  CLI_PATH             – path to Spotify.Slsk.Integration.Cli binary
                         (default: <repo-root>/dist/cli/linux-x64/Spotify.Slsk.Integration.Cli)
  CLI_TIMEOUT          – seconds to wait per download before giving up (default: 120)
  DRY_RUN              – set to "1" to print what would run without calling the CLI
"""

import os
import re
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

import gspread
from google.oauth2.service_account import Credentials

# ---------------------------------------------------------------------------
# Config
# ---------------------------------------------------------------------------

SHEET_ID = os.environ["GOOGLE_SHEET_ID"]
CREDS_PATH = os.environ["GOOGLE_CREDS_PATH"]
SOULSEEK_USERNAME = os.environ["SOULSEEK_USERNAME"]
SOULSEEK_PASSWORD = os.environ["SOULSEEK_PASSWORD"]

_repo_root = Path(__file__).resolve().parent.parent
_default_cli = _repo_root / "dist" / "cli" / "linux-x64" / "Spotify.Slsk.Integration.Cli"
CLI_PATH = Path(os.environ.get("CLI_PATH", str(_default_cli)))
CLI_TIMEOUT = int(os.environ.get("CLI_TIMEOUT", "120"))
DRY_RUN = os.environ.get("DRY_RUN", "0") == "1"

# Column indices (1-based to match gspread's update_cell)
COL_TRACK = 1
COL_ARTIST = 2
COL_DATE_ADDED = 3
COL_STATUS = 4
COL_PROCESSED_AT = 5
COL_URL = 6
COL_VIBE = 7

STATUS_NEW = "New"
STATUS_PROCESSING = "Processing"
STATUS_DOWNLOADED = "Downloaded"
STATUS_NOT_FOUND = "Not Found"

SCOPES = ["https://www.googleapis.com/auth/spreadsheets"]

# Matches "Track Name by Artist Name on Apple Music"
_APPLE_MUSIC_RE = re.compile(r"^(.+?)\s+by\s+(.+?)\s+on\s+Apple\s+Music$", re.IGNORECASE)

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def open_sheet() -> gspread.Worksheet:
    creds = Credentials.from_service_account_file(CREDS_PATH, scopes=SCOPES)
    client = gspread.authorize(creds)
    return client.open_by_key(SHEET_ID).sheet1


def cell(row: list[str], col: int) -> str:
    """Return stripped cell value or empty string if column doesn't exist."""
    return row[col - 1].strip() if len(row) >= col else ""


def build_query(row: list[str]) -> str:
    """
    Build a Soulseek search query from a row.

    Priority:
      1. If column A is "Track by Artist on Apple Music" → "Artist - Track"
      2. If column B (Artist) is non-empty → "Artist - Track"
      3. Fall back to column A as-is.
    """
    track_col = cell(row, COL_TRACK)
    artist_col = cell(row, COL_ARTIST)

    match = _APPLE_MUSIC_RE.match(track_col)
    if match:
        track_name, artist_name = match.group(1).strip(), match.group(2).strip()
        return f"{artist_name} {track_name}"

    if artist_col:
        return f"{artist_col} {track_col}"

    return track_col


def human_label(row: list[str]) -> str:
    """Short human-readable label for log output."""
    track_col = cell(row, COL_TRACK)
    match = _APPLE_MUSIC_RE.match(track_col)
    if match:
        return f"{match.group(2).strip()} - {match.group(1).strip()}"
    artist_col = cell(row, COL_ARTIST)
    return f"{artist_col} - {track_col}" if artist_col else track_col


def run_cli(query: str) -> tuple[bool, str]:
    """
    Run the CLI download-track command for *query*.
    Returns (success, failure_note).
    Exit codes: 0 = success, 1 = exception, 2 = download failed.
    """
    if DRY_RUN:
        print(f"  [DRY RUN] would run: {CLI_PATH} download-track --query {query!r}")
        return True, ""

    cmd = [str(CLI_PATH), "download-track", "--query", query]
    env = {
        **os.environ,
        "SOULSEEK_USERNAME": SOULSEEK_USERNAME,
        "SOULSEEK_PASSWORD": SOULSEEK_PASSWORD,
    }

    try:
        result = subprocess.run(cmd, env=env, timeout=CLI_TIMEOUT, capture_output=True, text=True)
    except subprocess.TimeoutExpired:
        return False, f"Timed out after {CLI_TIMEOUT}s"
    except Exception as exc:
        return False, str(exc)

    if result.returncode == 0:
        return True, ""

    stderr_lines = result.stderr.strip().splitlines()
    note = stderr_lines[-1] if stderr_lines else f"exit {result.returncode}"
    return False, note


def now_utc() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC")


# ---------------------------------------------------------------------------
# Main processing loop
# ---------------------------------------------------------------------------


def process(sheet: gspread.Worksheet) -> None:
    all_rows = sheet.get_all_values()
    if not all_rows:
        print("Sheet is empty.")
        return

    data_rows = all_rows[1:]  # skip header row

    new_rows = [
        (idx + 2, row)  # +2: convert 0-based data index to 1-based sheet row
        for idx, row in enumerate(data_rows)
        if cell(row, COL_STATUS) == STATUS_NEW
    ]

    if not new_rows:
        print("No new songs to process.")
        return

    print(f"Found {len(new_rows)} new song(s) to process.")

    for sheet_row, row in new_rows:
        query = build_query(row)
        label = human_label(row)

        if not query:
            print(f"  Row {sheet_row}: skipping — track column is empty.")
            sheet.update_cell(sheet_row, COL_STATUS, "Skipped")
            sheet.update_cell(sheet_row, COL_PROCESSED_AT, now_utc())
            continue

        print(f"  Row {sheet_row}: '{label}' → query: {query!r}")
        sheet.update_cell(sheet_row, COL_STATUS, STATUS_PROCESSING)

        success, note = run_cli(query)

        if success:
            sheet.update_cell(sheet_row, COL_STATUS, STATUS_DOWNLOADED)
            print(f"    ✓ Downloaded")
        else:
            sheet.update_cell(sheet_row, COL_STATUS, STATUS_NOT_FOUND)
            print(f"    ✗ Not Found: {note}")

        sheet.update_cell(sheet_row, COL_PROCESSED_AT, now_utc())


def main() -> None:
    missing = [
        v for v in ("GOOGLE_SHEET_ID", "GOOGLE_CREDS_PATH", "SOULSEEK_USERNAME", "SOULSEEK_PASSWORD")
        if not os.environ.get(v)
    ]
    if missing:
        print(f"Error: missing env vars: {', '.join(missing)}", file=sys.stderr)
        sys.exit(1)

    if not DRY_RUN and not CLI_PATH.exists():
        print(f"Error: CLI binary not found at {CLI_PATH}", file=sys.stderr)
        print("Run scripts/publish-cli.sh first, or set CLI_PATH.", file=sys.stderr)
        sys.exit(1)

    print(f"[{now_utc()}] Starting song processor (dry_run={DRY_RUN})")
    process(open_sheet())
    print(f"[{now_utc()}] Done.")


if __name__ == "__main__":
    main()
