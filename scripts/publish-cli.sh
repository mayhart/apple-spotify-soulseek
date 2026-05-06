#!/usr/bin/env bash
# ============================================================================
# publish-cli.sh — Publish Spotseek CLI as a self-contained executable
# ============================================================================
# Usage:
#   chmod +x scripts/publish-cli.sh
#   ./scripts/publish-cli.sh
#
# Output:
#   dist/cli/osx-arm64/Spotify.Slsk.Integration.Cli   (macOS Apple Silicon)
#   dist/cli/linux-arm64/Spotify.Slsk.Integration.Cli  (Linux aarch64, for cowork)
# ============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
PROJECT="$REPO_ROOT/Source/Assemblies/Spotify.Slsk.Integration.Cli/Spotify.Slsk.Integration.Cli.csproj"

publish() {
    local RID="$1"
    local OUT="$REPO_ROOT/dist/cli/$RID"
    echo "Publishing for $RID..."
    rm -rf "$OUT"
    mkdir -p "$OUT"
    dotnet publish "$PROJECT" \
        -r "$RID" \
        -c Release \
        --self-contained true \
        -p:PublishSingleFile=true \
        -o "$OUT"
    chmod +x "$OUT/Spotify.Slsk.Integration.Cli"
    echo "  -> $OUT/Spotify.Slsk.Integration.Cli"
}

publish "osx-arm64"
publish "linux-arm64"
publish "linux-x64"

echo "Done."
