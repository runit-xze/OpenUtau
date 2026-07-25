#!/usr/bin/env bash
# Driver for launching and poking the OpenUtau desktop app headlessly.
# Wraps: dotnet build/test, a nested Xephyr X server (so the app gets a real
# window instead of failing to find a display), and xdotool/import for input
# and screenshots.
#
# State (display number, PIDs) lives in $STATE_DIR so commands can be run as
# separate invocations from an agent (launch, then click, then screenshot, ...).
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
export PATH="$HOME/.dotnet:$PATH"
STATE_DIR="${OPENUTAU_DRIVER_STATE:-/tmp/openutau-driver}"
mkdir -p "$STATE_DIR"
DISPLAY_NUM="${OPENUTAU_DISPLAY_NUM:-77}"
DISPLAY_SPEC=":$DISPLAY_NUM"
XEPHYR_PID_FILE="$STATE_DIR/xephyr.pid"
APP_PID_FILE="$STATE_DIR/app.pid"
APP_LOG="$STATE_DIR/app.log"
SHOT_DIR="$STATE_DIR/screenshots"
mkdir -p "$SHOT_DIR"

cmd="${1:-}"
shift || true

case "$cmd" in
  build)
    dotnet build "$REPO_ROOT/OpenUtau/OpenUtau.csproj" -c Debug
    ;;

  test)
    dotnet test "$REPO_ROOT/OpenUtau.Test/OpenUtau.Test.csproj"
    ;;

  xephyr-up)
    if [ -f "$XEPHYR_PID_FILE" ] && kill -0 "$(cat "$XEPHYR_PID_FILE")" 2>/dev/null; then
      echo "Xephyr already running on $DISPLAY_SPEC (pid $(cat "$XEPHYR_PID_FILE"))"
    else
      Xephyr -ac -br -noreset -screen 1280x800 "$DISPLAY_SPEC" \
        > "$STATE_DIR/xephyr.log" 2>&1 &
      echo $! > "$XEPHYR_PID_FILE"
      sleep 1
      echo "Xephyr up on $DISPLAY_SPEC (pid $(cat "$XEPHYR_PID_FILE"))"
    fi
    ;;

  launch)
    "$0" xephyr-up
    if [ -f "$APP_PID_FILE" ] && kill -0 "$(cat "$APP_PID_FILE")" 2>/dev/null; then
      echo "OpenUtau already running (pid $(cat "$APP_PID_FILE"))"
    else
      DISPLAY="$DISPLAY_SPEC" nohup dotnet \
        "$REPO_ROOT/OpenUtau/bin/Debug/net8.0/OpenUtau.dll" \
        > "$APP_LOG" 2>&1 &
      echo $! > "$APP_PID_FILE"
      sleep 6
      if ! kill -0 "$(cat "$APP_PID_FILE")" 2>/dev/null; then
        echo "OpenUtau exited immediately, log:"
        cat "$APP_LOG"
        exit 1
      fi
      echo "OpenUtau launched (pid $(cat "$APP_PID_FILE")) on $DISPLAY_SPEC"
    fi
    ;;

  windows)
    DISPLAY="$DISPLAY_SPEC" xdotool search --onlyvisible --name "." \
      | while read -r w; do echo "$w: $(DISPLAY="$DISPLAY_SPEC" xdotool getwindowname "$w" 2>/dev/null)"; done
    ;;

  # Close a specific top-level window by X window id (see `windows`).
  # Use this for dialogs that have no visible close button (no WM decorations
  # under Xephyr) — e.g. the startup "Check for Update" window.
  windowclose)
    DISPLAY="$DISPLAY_SPEC" xdotool windowclose "$1"
    sleep 0.5
    ;;

  click)
    x="$1"; y="$2"
    DISPLAY="$DISPLAY_SPEC" xdotool mousemove "$x" "$y" click 1
    sleep 0.5
    ;;

  key)
    DISPLAY="$DISPLAY_SPEC" xdotool key "$1"
    sleep 0.5
    ;;

  type)
    DISPLAY="$DISPLAY_SPEC" xdotool type "$1"
    sleep 0.5
    ;;

  screenshot)
    name="${1:-shot-$(date +%s)}"
    out="$SHOT_DIR/$name.png"
    DISPLAY="$DISPLAY_SPEC" import -window root "$out"
    echo "$out"
    ;;

  log)
    cat "$APP_LOG" 2>/dev/null || echo "(no log yet)"
    ;;

  quit)
    if [ -f "$APP_PID_FILE" ]; then
      kill "$(cat "$APP_PID_FILE")" 2>/dev/null || true
      rm -f "$APP_PID_FILE"
    fi
    ;;

  down)
    "$0" quit
    if [ -f "$XEPHYR_PID_FILE" ]; then
      kill "$(cat "$XEPHYR_PID_FILE")" 2>/dev/null || true
      rm -f "$XEPHYR_PID_FILE"
    fi
    ;;

  *)
    cat <<EOF
Usage: driver.sh <command> [args]

Build/test (no display needed):
  build                  dotnet build the desktop app
  test                   run the OpenUtau.Test suite

Run/drive (needs Xephyr + xdotool + ImageMagick 'import'):
  xephyr-up              start the nested X server on :$DISPLAY_NUM (idempotent)
  launch                 xephyr-up, then start OpenUtau.dll against it
  windows                list visible top-level windows (id: title)
  windowclose <id>       close a window by id (dialogs have no WM close button)
  click <x> <y>          move mouse and left-click
  key <keysym>           send a key (xdotool syntax, e.g. Escape, alt+e)
  type <text>             type literal text
  screenshot [name]      screenshot the whole display, prints the path
  log                    print the app's stdout/stderr so far
  quit                   kill the app (Xephyr stays up)
  down                   kill the app and Xephyr

State dir: $STATE_DIR (override with OPENUTAU_DRIVER_STATE)
Display:   $DISPLAY_SPEC (override with OPENUTAU_DISPLAY_NUM)
EOF
    exit 1
    ;;
esac
