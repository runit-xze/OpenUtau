# Upstream backlog triage

Snapshot of every open issue and PR on `openutau/OpenUtau` at the time of triage,
with what this fork has done about it. Fork-only: nothing here was pushed or
commented upstream.

- Open PRs surveyed: **110** — 47 merged into this fork (Tier 1 + Tier 2)
- Open issues surveyed: **63**

## Issue status

### Fixed here (4)

| # | Title | Note |
|---|---|---|
| [2153](https://github.com/openutau/OpenUtau/issues/2153) | Crashes when I open Preferences | PR #58 - ONNX static-init poisoning crashed Preferences |
| [2154](https://github.com/openutau/OpenUtau/issues/2154) | The song starts to play then stops immediatly and gives this error | PR #59 - SharpWavtool negative offset / buffer shrink |
| [2229](https://github.com/openutau/OpenUtau/issues/2229) | Audio Doesn't Play | PR #60 - autosave ArgumentNullException on unsaved project |
| [2233](https://github.com/openutau/OpenUtau/issues/2233) | closes when i enter on options | PR #58 - exact stack trace reproduced and fixed |

### Partially fixed here (1)

| # | Title | Note |
|---|---|---|
| [2131](https://github.com/openutau/OpenUtau/issues/2131) | Whenever I use moresampler, this happens | PR #61 - defensive fix for dictionary race; no deterministic test |

### Likely fixed here (unconfirmed) (5)

Crash reports with no attached stack trace whose symptom class matches the
static-initializer poisoning fixed in PR #58. Unconfirmed without reporter logs.

| # | Title | Note |
|---|---|---|
| [2129](https://github.com/openutau/OpenUtau/issues/2129) | Crashes when clicking "new project" | crash class matches PR #58 Onnx poisoning; needs reporter log to confirm |
| [2143](https://github.com/openutau/OpenUtau/issues/2143) | Can’t access the piano roll feature | crash class matches PR #58 Onnx poisoning; needs reporter log to confirm |
| [2156](https://github.com/openutau/OpenUtau/issues/2156) | OpenUtau keeps crashing | crash class matches PR #58 Onnx poisoning; needs reporter log to confirm |
| [2158](https://github.com/openutau/OpenUtau/issues/2158) | openutau crashes when i select a phonemizer | crash class matches PR #58 Onnx poisoning; needs reporter log to confirm |
| [2195](https://github.com/openutau/OpenUtau/issues/2195) | openutau wont open after updating to newest version (0.1.568) | crash class matches PR #58 Onnx poisoning; needs reporter log to confirm |

### Needs reproduction (27)

Mostly short user reports ("it wont play") with no stack trace or project file.
Not actionable without a reproduction.

| # | Title | Note |
|---|---|---|
| [1466](https://github.com/openutau/OpenUtau/issues/1466) | Certain diphones are assigned to incorrect pitch on multisyllabic words in SBP p | no stack trace in report; needs reproduction |
| [1538](https://github.com/openutau/OpenUtau/issues/1538) | [JA VCV & CVVC] Phonemizer struggles to parse phrases involving atypical Kana or | no stack trace in report; needs reproduction |
| [1721](https://github.com/openutau/OpenUtau/issues/1721) | [JA Presamp] Presamp.ini breaks other voicebank in same runtime | no stack trace in report; needs reproduction |
| [1932](https://github.com/openutau/OpenUtau/issues/1932) | macOS: vLabeler path selector doesn't work with .app bundles | no stack trace in report; needs reproduction |
| [1933](https://github.com/openutau/OpenUtau/issues/1933) | vLabeler integration no longer brings its window into focus | no stack trace in report; needs reproduction |
| [2043](https://github.com/openutau/OpenUtau/issues/2043) | Transcribe audio offset when audio is cropped | no stack trace in report; needs reproduction |
| [2114](https://github.com/openutau/OpenUtau/issues/2114) | overwrite pitch tool is doing whatever this is | no stack trace in report; needs reproduction |
| [2134](https://github.com/openutau/OpenUtau/issues/2134) | Random crash while using diffsinger | no stack trace in report; needs reproduction |
| [2145](https://github.com/openutau/OpenUtau/issues/2145) | it wont play | no stack trace in report; needs reproduction |
| [2152](https://github.com/openutau/OpenUtau/issues/2152) | Duplicate external voicebank folder name fails to load | no stack trace in report; needs reproduction |
| [2161](https://github.com/openutau/OpenUtau/issues/2161) | Kasane Teto's "I" phoneme distorted from resampler | no stack trace in report; needs reproduction |
| [2164](https://github.com/openutau/OpenUtau/issues/2164) | Dual expression preview no longer works in beta 0.1.568.0+ | no stack trace in report; needs reproduction |
| [2167](https://github.com/openutau/OpenUtau/issues/2167) | OpenUtau wont play no matter what i try | no stack trace in report; needs reproduction |
| [2172](https://github.com/openutau/OpenUtau/issues/2172) | Switching from DiffSinger to legacy CV/CVVC singer on the same track causes pitc | no stack trace in report; needs reproduction |
| [2177](https://github.com/openutau/OpenUtau/issues/2177) | when I try to intonate a vocaloid the site glitches and closes by itself | no stack trace in report; needs reproduction |
| [2181](https://github.com/openutau/OpenUtau/issues/2181) | Switched to WORLDLINE-R, this happened | no stack trace in report; needs reproduction |
| [2188](https://github.com/openutau/OpenUtau/issues/2188) | crashes when i place down a note and set a singer when using a ust/ustx | no stack trace in report; needs reproduction |
| [2192](https://github.com/openutau/OpenUtau/issues/2192) | wont play | no stack trace in report; needs reproduction |
| [2193](https://github.com/openutau/OpenUtau/issues/2193) | Play Header not moving | no stack trace in report; needs reproduction |
| [2201](https://github.com/openutau/OpenUtau/issues/2201) | Moresampler fails to generate new LLSM files | no stack trace in report; needs reproduction |
| [2202](https://github.com/openutau/OpenUtau/issues/2202) | it wont play at all or move | no stack trace in report; needs reproduction |
| [2206](https://github.com/openutau/OpenUtau/issues/2206) | when i press the play button it dosent play | no stack trace in report; needs reproduction |
| [2211](https://github.com/openutau/OpenUtau/issues/2211) | It wont play and always crashes | no stack trace in report; needs reproduction |
| [2239](https://github.com/openutau/OpenUtau/issues/2239) | playhead thing wont play but audio does (0.1.568 Beta) | no stack trace in report; needs reproduction |
| [2240](https://github.com/openutau/OpenUtau/issues/2240) | HELP - | no stack trace in report; needs reproduction |
| [2256](https://github.com/openutau/OpenUtau/issues/2256) | Failed to Render? | no stack trace in report; needs reproduction |
| [2264](https://github.com/openutau/OpenUtau/issues/2264) | Timing issue of 48 kHz sample rate audio | no stack trace in report; needs reproduction |

### Feature request (26)

| # | Title | Note |
|---|---|---|
| [1414](https://github.com/openutau/OpenUtau/issues/1414) | Convert pitch control points to PITD |  |
| [1415](https://github.com/openutau/OpenUtau/issues/1415) | Set editor-wide portrait height and opacity in Preferences |  |
| [1417](https://github.com/openutau/OpenUtau/issues/1417) | "Sanitize" option for distributing .ustx |  |
| [1510](https://github.com/openutau/OpenUtau/issues/1510) | Add GPU support for DiffSinger in Linux |  |
| [1664](https://github.com/openutau/OpenUtau/issues/1664) | Support selection of multiple pitch points |  |
| [1833](https://github.com/openutau/OpenUtau/issues/1833) | G2P Trainer - Request to update or revisit code |  |
| [1881](https://github.com/openutau/OpenUtau/issues/1881) | 一個有關oto的請求     A request for oto |  |
| [1897](https://github.com/openutau/OpenUtau/issues/1897) | Stereo Exports? |  |
| [1945](https://github.com/openutau/OpenUtau/issues/1945) | support using default device in system settings |  |
| [1946](https://github.com/openutau/OpenUtau/issues/1946) | C+V spanish phonemizer |  |
| [1960](https://github.com/openutau/OpenUtau/issues/1960) | [Idea] Just adding a "delete singer" button |  |
| [1976](https://github.com/openutau/OpenUtau/issues/1976) | IPA Phonemizers |  |
| [1978](https://github.com/openutau/OpenUtau/issues/1978) | WASM Port of OpenUtau |  |
| [1997](https://github.com/openutau/OpenUtau/issues/1997) | Thai Diffsinger Phonemizer |  |
| [2005](https://github.com/openutau/OpenUtau/issues/2005) | Improved piano roll scale highlighting |  |
| [2050](https://github.com/openutau/OpenUtau/issues/2050) | Negative timing for entire project |  |
| [2052](https://github.com/openutau/OpenUtau/issues/2052) | Adding markers in piano roll |  |
| [2120](https://github.com/openutau/OpenUtau/issues/2120) | Latin choir |  |
| [2123](https://github.com/openutau/OpenUtau/issues/2123) | Easier way to find voicebanks |  |
| [2160](https://github.com/openutau/OpenUtau/issues/2160) | Automatic detection of voicebank type |  |
| [2184](https://github.com/openutau/OpenUtau/issues/2184) | [JA VCV&CVVC] Enhancing Test Code |  |
| [2194](https://github.com/openutau/OpenUtau/issues/2194) | UI Color Distinguishability Impacts Efficiency |  |
| [2219](https://github.com/openutau/OpenUtau/issues/2219) | Move tempo markers via click and drag |  |
| [2252](https://github.com/openutau/OpenUtau/issues/2252) | Add Vietnamese to Japanese (VIE to JA) Phonemizer |  |
| [2271](https://github.com/openutau/OpenUtau/issues/2271) | Please add keyboard shortcut for "Export Audio > Export Wave Files" |  |
| [2273](https://github.com/openutau/OpenUtau/issues/2273) | Will Turkish Language be get added on Crowdin? |  |

