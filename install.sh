#!/bin/sh
# XDG-compliant install script for OpenUtau AppImage.
# Usage: ./install.sh [path-to-AppImage]
# Default: looks for OpenUtau-linux-x86_64.AppImage in current directory.

set -e

APPIMAGE="${1:-OpenUtau-linux-x86_64.AppImage}"

if [ ! -f "$APPIMAGE" ]; then
	echo "Error: $APPIMAGE not found." >&2
	echo "Usage: $0 [path-to-AppImage]" >&2
	exit 1
fi

# XDG base directories
BIN_DIR="${HOME}/.local/bin"
APP_DIR="${HOME}/.local/share/applications"
ICON_DIR="${HOME}/.local/share/icons"

mkdir -p "$BIN_DIR" "$APP_DIR" "$ICON_DIR"

install -m 755 "$APPIMAGE" "$BIN_DIR/OpenUtau"
echo "Installed AppImage → $BIN_DIR/OpenUtau"

# .desktop file — tries project source first, then falls back
DESKTOP_SRC="OpenUtau/Assets/openutau.desktop"
if [ -f "$DESKTOP_SRC" ]; then
	sed "s|^Exec=.*|Exec=$BIN_DIR/OpenUtau|" "$DESKTOP_SRC" > "$APP_DIR/openutau.desktop"
else
	cat > "$APP_DIR/openutau.desktop" <<-EOF
	[Desktop Entry]
	Type=Application
	Name=OpenUtau
	Exec=$BIN_DIR/OpenUtau
	Icon=openutau
	Categories=Audio;Music;AudioVideo;
	Terminal=false
	EOF
fi
chmod 644 "$APP_DIR/openutau.desktop"
echo "Installed .desktop → $APP_DIR/openutau.desktop"

# Icon
ICON_SRC="OpenUtau/Assets/logotype.png"
if [ -f "$ICON_SRC" ]; then
	install -m 644 "$ICON_SRC" "$ICON_DIR/openutau.png"
	echo "Installed icon → $ICON_DIR/openutau.png"
else
	echo "Warning: icon not found at $ICON_SRC; skipping." >&2
fi

# Refresh desktop database (non-fatal if unavailable)
if command -v update-desktop-database >/dev/null 2>&1; then
	update-desktop-database "$APP_DIR" 2>/dev/null || true
fi

echo ""
echo "OpenUtau installed. Run with: $BIN_DIR/OpenUtau"
echo "Or launch from your application menu."
