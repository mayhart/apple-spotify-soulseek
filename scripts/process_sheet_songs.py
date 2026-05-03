#!/usr/bin/env python3
"""
Reads a Google Sheet for new song entries, downloads each via the Spotseek CLI,
and writes the result status back to the sheet.

Expected sheet columns (1-indexed):
  A  Song Name
  B  Artist
  C  Query       (optional; if empty, built as "Artist - Song Name")
  D  Status      (empty = new; set to Processing → Downloaded / Failed)
  E  Notes       (failure reason or other info)
  F  Processed At

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

COL_SONG = 1
COL_ARTIST = 2
COL_QUERY = 3
COL_STATUS = 4
COL_NOTES = 5
COL_PROCESSED_AT = 6

STATUS_NEW = ""
STATUS_PROCESSING = "Processing"
STATUS_DOWNLOADED = "Downloaded"
STATUS_FAILED = "Failed"

SCOPES = [
    "https://www.googleapis.com/auth/spreadsheets",
]

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def open_sheet() -> gspread.Worksheet:
    creds = Credentials.from_service_account_file(CREDS_PATH, scopes=SCOPES)
    client = gspread.authorize(creds)
    return client.open_by_key(SHEET_ID).sheet1


def build_query(row: list[str]) -> str:
    """Return the search query for a row, defaulting to 'Artist - Song Name'."""
    query = row[COL_QUERY - 1].strip() if len(row) >= COL_QUERY else ""
    if query:
        return query
    artist = row[COL_ARTIST - 1].strip() if len(row) >= COL_ARTIST else ""
    song = row[COL_SONG - 1].strip() if len(row) >= COL_SONG else ""
    return f"{artist} - {song}" if artist else song


def run_cli(query: str) -> tuple[bool, str]:
    """
    Run the CLI download-track command for *query*.
    Returns (success, notes).
    Exit codes: 0 = success, 1 = exception, 2 = download failed.
    """
    if DRY_RUN:
        print(f"  [DRY RUN] would run: {CLI_PATH} download-track -q {query!r}")
        return True, "dry-run"

    cmd = [
        str(CLI_PATH),
        "download-track",
        "--query", query,
    ]
    env = {
        **os.environ,
        "SOULSEEK_USERNAME": SOULSEEK_USERNAME,
        "SOULSEEK_PASSWORD": SOULSEEK_PASSWORD,
    }

    try:
        result = subprocess.run(
            cmd,
            env=env,
            timeout=CLI_TIMEOUT,
            capture_output=True,
            text=True,
        )
    except subprocess.TimeoutExpired:
        return False, f"Timed out after {CLI_TIMEOUT}s"
    except Exception as exc:
        return False, str(exc)

    if result.returncode == 0:
        return True, ""

    stderr = result.stderr.strip().splitlines()
    note = stderr[-1] if stderr else f"exit code {result.returncode}"
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

    # Row 1 is the header; data starts at row index 1 (1-based row 2)
    header = all_rows[0]
    data_rows = all_rows[1:]

    new_rows = [
        (row_idx + 2, row)  # +2: 1-based index + skip header
        for row_idx, row in enumerate(data_rows)
        if len(row) < COL_STATUS or row[COL_STATUS - 1].strip() in (STATUS_NEW,)
    ]

    if not new_rows:
        print("No new songs to process.")
        return

    print(f"Found {len(new_rows)} new song(s) to process.")

    for sheet_row, row in new_rows:
        song = row[COL_SONG - 1].strip() if len(row) >= COL_SONG else ""
        artist = row[COL_ARTIST - 1].strip() if len(row) >= COL_ARTIST else ""
        query = build_query(row)

        if not query:
            print(f"  Row {sheet_row}: skipping — no song name or query.")
            sheet.update_cell(sheet_row, COL_STATUS, "Skipped")
            sheet.update_cell(sheet_row, COL_NOTES, "No song name or query")
            sheet.update_cell(sheet_row, COL_PROCESSED_AT, now_utc())
            continue

        label = f"{artist} - {song}" if artist else song
        print(f"  Row {sheet_row}: processing '{label}' (query: {query!r})")

        sheet.update_cell(sheet_row, COL_STATUS, STATUS_PROCESSING)

        success, notes = run_cli(query)
        status = STATUS_DOWNLOADED if success else STATUS_FAILED

        sheet.update_cell(sheet_row, COL_STATUS, status)
        if notes:
            sheet.update_cell(sheet_row, COL_NOTES, notes)
        sheet.update_cell(sheet_row, COL_PROCESSED_AT, now_utc())

        result_icon = "✓" if success else "✗"
        print(f"    {result_icon} {status}" + (f": {notes}" if notes else ""))


def main() -> None:
    missing = [
        var for var in ("GOOGLE_SHEET_ID", "GOOGLE_CREDS_PATH", "SOULSEEK_USERNAME", "SOULSEEK_PASSWORD")
        if not os.environ.get(var)
    ]
    if missing:
        print(f"Error: missing required environment variables: {', '.join(missing)}", file=sys.stderr)
        sys.exit(1)

    if not DRY_RUN and not CLI_PATH.exists():
        print(f"Error: CLI binary not found at {CLI_PATH}", file=sys.stderr)
        print("Run scripts/publish-cli.sh first, or set CLI_PATH.", file=sys.stderr)
        sys.exit(1)

    print(f"[{now_utc()}] Starting song processor (dry_run={DRY_RUN})")

    sheet = open_sheet()
    process(sheet)

    print(f"[{now_utc()}] Done.")


if __name__ == "__main__":
    main()
