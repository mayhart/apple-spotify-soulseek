#!/usr/bin/env bash
# Publishes the Spotseek CLI as a self-contained binary for Linux.
# Output: dist/cli/linux-x64/Spotify.Slsk.Integration.Cli

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
PROJECT="$REPO_ROOT/Source/Assemblies/Spotify.Slsk.Integration.Cli/Spotify.Slsk.Integration.Cli.csproj"
OUTPUT_DIR="$REPO_ROOT/dist/cli/linux-x64"

echo "Publishing Spotseek CLI for linux-x64..."
dotnet publish "$PROJECT" \
    -r linux-x64 \
    -c Release \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o "$OUTPUT_DIR"

chmod +x "$OUTPUT_DIR/Spotify.Slsk.Integration.Cli"
echo "Done: $OUTPUT_DIR/Spotify.Slsk.Integration.Cli"
