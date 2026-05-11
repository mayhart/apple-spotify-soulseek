#!/usr/bin/env bash
# Installs (or replaces) a cron job that runs process_sheet_songs.py
# every Monday at 09:00 local time.
#
# Usage:
#   chmod +x scripts/setup_cron.sh
#   ./scripts/setup_cron.sh
#
# Prereqs:
#   pip3 install -r scripts/requirements_sheet.txt
#   ./scripts/publish-cli.sh
#
# Required env vars:
#   SOULSEEK_USERNAME
#   SOULSEEK_PASSWORD
#
# Optional:
#   SONGS_FILE  – path to CSV (default: ~/Library/Mobile Documents/com~apple~CloudDocs/spotseek-songs.csv)
#   CLI_PATH    – CLI binary path (auto-detected by platform if unset)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
PYTHON="${PYTHON:-$(command -v python3)}"
PROCESSOR="$SCRIPT_DIR/process_sheet_songs.py"
LOG_FILE="$REPO_ROOT/song-processor.log"
CRON_TAG="# spotseek-sheet-processor"

for var in SOULSEEK_USERNAME SOULSEEK_PASSWORD; do
    if [[ -z "${!var:-}" ]]; then
        echo "Error: $var is not set. Export it before running this script." >&2
        exit 1
    fi
done

if [[ -z "${CLI_PATH:-}" ]]; then
    case "$(uname -s)-$(uname -m)" in
        Darwin-arm64)  CLI_PATH="$REPO_ROOT/dist/cli/osx-arm64/Spotify.Slsk.Integration.Cli" ;;
        Darwin-x86_64) CLI_PATH="$REPO_ROOT/dist/cli/osx-x64/Spotify.Slsk.Integration.Cli" ;;
        Linux-aarch64) CLI_PATH="$REPO_ROOT/dist/cli/linux-arm64/Spotify.Slsk.Integration.Cli" ;;
        *)             CLI_PATH="$REPO_ROOT/dist/cli/linux-x64/Spotify.Slsk.Integration.Cli" ;;
    esac
fi

if [[ -z "${SONGS_FILE:-}" ]]; then
    SONGS_FILE="$HOME/Library/Mobile Documents/com~apple~CloudDocs/spotseek-songs.csv"
fi

echo "Using CLI binary: $CLI_PATH"
echo "Using songs file: $SONGS_FILE"

if [[ ! -f "$CLI_PATH" ]]; then
    echo "Error: CLI binary not found at $CLI_PATH" >&2
    echo "Run ./scripts/publish-cli.sh first." >&2
    exit 1
fi

CRON_LINE="0 9 * * 1 SOULSEEK_USERNAME=$SOULSEEK_USERNAME SOULSEEK_PASSWORD=$SOULSEEK_PASSWORD CLI_PATH=$CLI_PATH SONGS_FILE=\"$SONGS_FILE\" $PYTHON $PROCESSOR >> $LOG_FILE 2>&1 $CRON_TAG"

( crontab -l 2>/dev/null | grep -v "$CRON_TAG"; echo "$CRON_LINE" ) | crontab -

echo "Cron job installed. Current crontab:"
crontab -l | grep "$CRON_TAG"
echo ""
echo "The job will run every Monday at 09:00."
echo "Logs will be written to: $LOG_FILE"
