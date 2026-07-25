---
name: run-openutau
description: Build, test, run, and drive the OpenUtau desktop app (Avalonia/.NET 8). Use when asked to build OpenUtau, run its tests, start the app, take a screenshot of its UI, or click through the singing-synthesis editor.
---

OpenUtau is a native .NET 8 / Avalonia desktop GUI (not a web app, not Electron —
no Playwright/CDP hook exists for it). Most PRs touch `OpenUtau.Core` or
`OpenUtau.Plugin.Builtin` (phonemizers, rendering, format parsing) and are best
verified with `dotnet test` directly — no display needed. PRs that touch
`OpenUtau/` (the UI project: `Views/`, `Controls/`, `ViewModels/`) need the GUI
driver at `.claude/skills/run-openutau/driver.sh`, which launches the app inside
a nested Xephyr X server and drives it with `xdotool` + ImageMagick `import`.

All paths below are relative to the repo root (`openutau-ref/`).

## Prerequisites

```bash
sudo apt-get install -y xvfb xserver-xephyr xdotool imagemagick
```

.NET 8 SDK (not installed by default on this box):

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel 8.0 --install-dir "$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"   # add to shell profile to persist
```

## Build

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet restore OpenUtau.sln
dotnet build OpenUtau/OpenUtau.csproj -c Debug
```

Or via the driver: `.claude/skills/run-openutau/driver.sh build`

Expect ~1600 nullable-reference warnings (pre-existing, `TreatWarningsAsErrors`
is only enforced on `OpenUtau.csproj`, not `OpenUtau.Core`) and 3 harmless
`AVLN3001` "no public constructor" warnings for `PianoRoll`,
`PianoRollDetachedWindow`, and `ThemeEditorWindow` — those types are
deliberately constructed from code (one is a singleton), never by the XAML
runtime loader, so the warning is a false positive. Zero errors is the bar.

## Run (agent path)

Drive it through `driver.sh` — every command below was run against this repo
and confirmed working:

```bash
D=.claude/skills/run-openutau/driver.sh
"$D" launch                    # starts Xephyr on :77, then launches OpenUtau.dll
"$D" windows                   # lists top-level windows: "<id>: <title>"
"$D" windowclose <id>          # closes a window by id (see Gotchas — no WM, no close button)
"$D" click <x> <y>             # move mouse + left-click, screen coords
"$D" key <keysym>              # xdotool key syntax, e.g. Escape, alt+e
"$D" type "<text>"             # type literal text into the focused field
"$D" screenshot [name]         # screenshots the whole display, prints the saved path
"$D" log                       # prints the app's stdout/stderr captured so far
"$D" quit                      # kills the app; Xephyr stays up
"$D" down                      # kills the app and Xephyr
```

Screenshots land in `/tmp/openutau-driver/screenshots/<name>.png` (override the
whole state dir with `OPENUTAU_DRIVER_STATE`; override the display number with
`OPENUTAU_DISPLAY_NUM`, default `77`).

Typical session — launch, dismiss the startup update-check dialog, create a
project, open Preferences:

```bash
D=.claude/skills/run-openutau/driver.sh
"$D" launch
"$D" windows                       # → "<id1>: OpenUtau v0.0.0.0" / "<id2>: Check for Update"
"$D" windowclose <id2>             # dismiss the update dialog
"$D" click 166 172                 # "New" tile → creates an untitled project
"$D" screenshot piano-roll
"$D" down
```

### Direct invocation (preferred for Core/Plugin.Builtin changes)

No display needed — this is the harness to reach for when a change is in a
phonemizer, the renderer, or file-format parsing:

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test OpenUtau.Test/OpenUtau.Test.csproj
# or a single fixture while iterating:
dotnet test OpenUtau.Test/OpenUtau.Test.csproj --filter "FullyQualifiedName~EnVCCVTest"
```

## Run (human path)

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet OpenUtau/bin/Debug/net8.0/OpenUtau.dll
```

Opens a normal window if you have a real display. Useless headless — this is
what `driver.sh launch` wraps with Xephyr.

## Test

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test OpenUtau.Test/OpenUtau.Test.csproj
```

217 tests, all passing as of this writing (~2 min). Covers `App`, `Classic`
(UST/flags parsing), `Core`, `Files`, `Plugins` (phonemizers), `Usts`.

---

## Gotchas

- **No window manager in Xephyr → no titlebar, no close button on dialogs.**
  The startup "Check for Update" window (`UpdaterDialog.axaml`, a real
  top-level `Window`, not an overlay) has no visible way to dismiss it once
  Xephyr's running — `Escape` and light-dismiss clicks do nothing. Use
  `driver.sh windows` to get its X window id, then `driver.sh windowclose
  <id>`. Same trick applies to any other undecorated top-level window that
  blocks input.
- **Preferences is under Tools → Preferences..., not Edit.** The Edit menu
  only has Undo/Redo.
- **`dotnet` isn't on `PATH` after `dotnet-install.sh`** — the installer only
  updates `PATH` for the shell that sourced it, not for future shells/tool
  calls. Every command in this skill explicitly does
  `export PATH="$HOME/.dotnet:$PATH"` first; don't assume it's already there.
- **`dotnet build` on `OpenUtau/OpenUtau.csproj` alone silently restores all
  4 projects in the solution** the first time — if you see "3 of 4 projects
  are up-to-date for restore" and then missing-package errors for
  `OpenUtau.Core`, run `dotnet restore OpenUtau.sln` explicitly rather than
  trusting the implicit restore.

## Troubleshooting

- **Build error `CS0246: 'Newtonsoft'`/`'SharpGen'` could not be found**:
  these packages are referenced by `EditTool.cs` /
  `DiffSingerBasePhonemizer.cs` but (at least as of the fork's current state)
  need explicit `<PackageReference>` entries in `OpenUtau.Core.csproj` — add
  `Newtonsoft.Json` and `SharpGen.Runtime` if they're missing.
- **App crashes immediately on Tools → Preferences with
  `ArgumentOutOfRangeException` in `PreferencesViewModel..ctor`**: this
  container has no GPU, so `Onnx.getGpuInfo()` returns an empty list;
  `PreferencesViewModel` indexing `OnnxGpuOptions[0]` unconditionally throws
  on an empty list. Already fixed in this fork (see
  `PreferencesViewModel.cs` — guarded with `OnnxGpuOptions.Count > 0`); if
  it resurfaces after a merge, that's the shape of the fix.
- **`xdotool windowactivate`/`windowfocus` errors with "windowmanager claims
  not to support _NET_ACTIVE_WINDOW"**: expected — there's no WM running in
  Xephyr. Plain `xdotool mousemove <x> <y> click 1` still works fine without
  activation; don't bother trying to focus/activate windows first.
- **`import -window root` produces a screenshot with a large black margin**:
  normal — that's the unused area of the Xephyr virtual screen
  (1280x800) outside the app window. Not a bug.
