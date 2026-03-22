#!/usr/bin/env bash
# ============================================================================
# build-mac.sh — Build Spotseek for macOS and package as a .dmg installer
# ============================================================================
# Requirements:
#   - .NET 8 SDK  (https://dot.net)
#   - Xcode command-line tools  (xcode-select --install)
#   - create-dmg  (brew install create-dmg)   [optional, falls back to hdiutil]
#
# Usage:
#   chmod +x scripts/build-mac.sh
#   ./scripts/build-mac.sh
# ============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
PROJECT="$REPO_ROOT/Source/Assemblies/Spotify.Slsk.Integration.Desktop/Spotify.Slsk.Integration.Desktop.csproj"
OUTPUT_DIR="$REPO_ROOT/dist/mac"
APP_NAME="Spotseek"
BUNDLE_ID="com.spotseek.app"
VERSION="1.0.0"

echo "🎵  Building $APP_NAME for macOS..."

# ── Clean ──────────────────────────────────────────────────────────────────
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

build_arch() {
    local RID="$1"
    local ARCH_LABEL="$2"
    local PUBLISH_DIR="$OUTPUT_DIR/publish-$RID"
    local APP_BUNDLE="$OUTPUT_DIR/$APP_NAME-$ARCH_LABEL.app"

    echo "  → Publishing for $RID..."
    dotnet publish "$PROJECT" \
        -r "$RID" \
        -c Release \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -o "$PUBLISH_DIR"

    # ── Create .app bundle ────────────────────────────────────────────────
    mkdir -p "$APP_BUNDLE/Contents/MacOS"
    mkdir -p "$APP_BUNDLE/Contents/Resources"

    cp "$PUBLISH_DIR/Spotseek" "$APP_BUNDLE/Contents/MacOS/Spotseek"
    chmod +x "$APP_BUNDLE/Contents/MacOS/Spotseek"

    # Copy icon if present
    if [ -f "$REPO_ROOT/Source/Assemblies/Spotify.Slsk.Integration.Desktop/Assets/spotseek.icns" ]; then
        cp "$REPO_ROOT/Source/Assemblies/Spotify.Slsk.Integration.Desktop/Assets/spotseek.icns" \
           "$APP_BUNDLE/Contents/Resources/spotseek.icns"
    fi

    cat > "$APP_BUNDLE/Contents/Info.plist" << PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleIdentifier</key>      <string>$BUNDLE_ID</string>
    <key>CFBundleName</key>            <string>$APP_NAME</string>
    <key>CFBundleDisplayName</key>     <string>$APP_NAME</string>
    <key>CFBundleVersion</key>         <string>$VERSION</string>
    <key>CFBundleShortVersionString</key> <string>$VERSION</string>
    <key>CFBundleExecutable</key>      <string>Spotseek</string>
    <key>CFBundlePackageType</key>     <string>APPL</string>
    <key>CFBundleIconFile</key>        <string>spotseek</string>
    <key>NSHighResolutionCapable</key> <true/>
    <key>LSMinimumSystemVersion</key>  <string>11.0</string>
</dict>
</plist>
PLIST

    # ── Package as .dmg ───────────────────────────────────────────────────
    local DMG_PATH="$OUTPUT_DIR/$APP_NAME-$VERSION-$ARCH_LABEL.dmg"
    echo "  → Creating $DMG_PATH..."

    if command -v create-dmg &>/dev/null; then
        create-dmg \
            --volname "$APP_NAME" \
            --window-size 540 380 \
            --icon-size 128 \
            --icon "$APP_NAME-$ARCH_LABEL.app" 130 180 \
            --app-drop-link 400 180 \
            "$DMG_PATH" \
            "$OUTPUT_DIR/"
    else
        # Fallback: plain hdiutil
        hdiutil create \
            -volname "$APP_NAME" \
            -srcfolder "$APP_BUNDLE" \
            -ov \
            -format UDZO \
            "$DMG_PATH"
    fi

    echo "  ✓ $DMG_PATH"
}

build_arch "osx-x64"   "intel"
build_arch "osx-arm64" "apple-silicon"

echo ""
echo "✅  Done! Installers are in $OUTPUT_DIR/"
ls -lh "$OUTPUT_DIR/"*.dmg
