#!/usr/bin/env bash
# Publishes the Spotseek CLI as a self-contained binary for multiple platforms.
# Output: dist/cli/<rid>/Spotify.Slsk.Integration.Cli

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
