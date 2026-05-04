# KeepSessionAlive — Claude Code Project Guide

> .NET Framework 4.8 WinForms desktop productivity utility
> Status: Feature-complete, maintenance mode
> Last updated: 2026-05-04

## Quick Build

1. Open `KeepSessionAlive.sln` in Visual Studio 2022
2. Restore NuGet packages (ScreenRecorderLib 6.6.0, DocumentFormat.OpenXml 2.20.0, InputSimulator 1.0.4.0)
3. Target: .NET Framework 4.8, x64
4. `Resources/Font Awesome 7 Free-Solid-900.otf` must have Build Action = `Embedded Resource`

## Architecture

Single WinForms partial class (`MainForm.cs`) — by design, user copies the file between machines.

```
KeepSessionAlive/
  MainForm.cs              # All main logic (1300+ lines)
  MainForm.Designer.cs     # UI layout (hand-authored, no VS designer)
  PptxExporter.cs          # PowerPoint export (separated to avoid ImageFormat namespace conflict)
  AdLookupForm.cs          # Active Directory lookup form
  Program.cs               # Entry point
  Resources/
    Font Awesome 7 Free-Solid-900.otf  # Embedded icon font
  packages.config
```

## Features

| Feature | Entry point | Key detail |
|---------|------------|-----------|
| Keep Online (mouse jiggler) | `statusOnline_Click` | Right-click at screen centre every 5 min idle; checks every 10 s |
| App time tracker | `IdleDisplayTimer_Tick` | DataGridView bar chart, bar refresh every 20 s |
| Screen recording | `statusRecord_Click` | ScreenRecorderLib, H264 Baseline, 4 Mbps CBR, 24 fps, mic + system audio |
| Screen capture → PPTX | `statusCapture_Click` | Multi-region overlay, OpenXML 16:9 export |
| Snap windows to grid | `SnapWindowsToGrid` | Up to 4 per screen, multi-monitor, skips cloaked/UWP |
| Lock screen | `statusLock_Click` | Win32 `LockWorkStation()` |
| AD Lookup | `AdLookupForm.cs` | ONEADR / QAONEADR domain, group ↔ user drill-down |

## Key Conventions

- All P/Invoke declarations at top of `MainForm.cs`
- Thread-safe UI updates via `InvokeRequired` / `BeginInvoke` — never touch controls from background threads
- FontAwesome icons loaded from embedded resource via `PrivateFontCollection` + `AddFontMemResourceEx` (GDI registration required)
- Dark theme applied once in `ApplyDarkTheme()` — bg `#1C1C1C`, surface `#2D2D2D`, accent `#FF8C00`, text `#DCDCDC`
- App minimizes to tray on `WindowState = Minimized`; double-click tray icon to restore
- `_cts` null = stopped, non-null = running (keep-online state machine)

## UI Architecture

### Status Strip (bottom, 40 px)
All icons are `ToolStripStatusLabel` with `IsLink = true`, `LinkBehavior = NeverUnderline`, FontAwesome 14 pt.

| Position | Icon codepoint | Function |
|----------|---------------|---------|
| Left | `` | Keep Online toggle — gray=off, LimeGreen=on |
| Left | `` | Toggle log panel (adds 116 px height) |
| Left | `` | Lock screen |
| Left | `` | Snap windows to quadrants |
| Left | `` | Capture screen region |
| Left | `` | Export captures to PowerPoint (hidden until first capture) |
| Spring | — | Pushes record button to far right |
| Right | `` / `` | Start / stop recording |

### Idle / Work time detection
- `GetLastInputInfo` (Win32) returns last input tick
- Idle threshold: 5 min for keep-alive trigger; 10 s for idle-display counter
- `_idleDisplayTimer` fires every 1 s on the UI thread — increments either `_totalWorkSeconds` or `_totalIdleSeconds`

### Browser domain detection
`GetBrowserDomain()` uses UI Automation to read the address bar Edit control of Chrome / Edge / Firefox / Opera / Brave / Vivaldi and strips it to bare hostname.

---

## Critical Issues Found — 2026-05-04

### 🔴 CRITICAL

#### [C1] GDI handle leak in `BuildTrayIcon()` (`MainForm.cs`)

`bmp.GetHicon()` creates a raw Win32 HICON handle. `Icon.FromHandle()` wraps it but **does not own or free it**. The HICON leaks on every call. Because `BuildTrayIcon()` is only called once at startup the leak is small, but it is technically incorrect.

```csharp
// Current (leaks the HICON):
IntPtr hIcon = bmp.GetHicon();
return Icon.FromHandle(hIcon);

// Fix — clone into managed memory then destroy the raw handle:
[DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr hIcon);

IntPtr hIcon = bmp.GetHicon();
var icon = (Icon)Icon.FromHandle(hIcon).Clone(); // copies pixels into managed Icon
DestroyIcon(hIcon);
return icon;
```

#### [C2] Dead code — `runQuery()` method (`MainForm.cs`)

`runQuery()` drives SSMS via hardcoded absolute pixel coordinates and `InputSimulator`. It is never wired to any button, menu item, or keyboard shortcut and is unreachable from normal use. It should either be removed or connected to a UI trigger.

---

### 🟠 HIGH

#### [H1] `Thread.Sleep(300)` blocks the UI thread (`StartRegionCapture()`, `MainForm.cs`)

`System.Threading.Thread.Sleep(300)` is called on the UI thread to allow the window to hide before taking the background screenshot. This makes the UI completely unresponsive for 300 ms. The method should be made `async` and use `await Task.Delay(300)`.

```csharp
// Before:
private void StartRegionCapture() { ... Thread.Sleep(300); ... }

// After:
private async void StartRegionCapture() { ... await Task.Delay(300); ... }
```

#### [H2] Private key committed to repository (`KeepSessionAlive_4_TemporaryKey.pfx`)

A code-signing `.pfx` file is tracked in source control. Even temporary keys should not be in a repository — they can be extracted and used to sign other binaries. Add to `.gitignore` and, if the key was ever shared or accessible, rotate it.

---

### 🟡 MEDIUM

#### [M1] `RecordAppSecond()` is O(n) every second (`MainForm.cs`)

Every second the timer iterates all DataGridView rows to find the matching app name row. With many tracked apps this is wasteful. Fix: maintain a parallel `Dictionary<string, int> _appRowIndex` mapping app name → row index.

```csharp
private readonly Dictionary<string, int> _appRowIndex = new();

// In RecordAppSecond():
if (!_appSeconds.ContainsKey(appName))
{
    _appSeconds[appName] = 0;
    _appRowIndex[appName] = dataGridView1.Rows.Add(appName, "0:00:00");
}
// Update row directly — no iteration needed:
dataGridView1.Rows[_appRowIndex[appName]].Cells["colTime"].Value = formatted;
```

#### [M2] README vs code mismatch — recording settings

`README.md` documents recording as "half-resolution, 2 Mbps, 15 fps, variable framerate, system audio disabled". The actual `StartRecording()` uses **full primary screen resolution, 4 Mbps CBR, 24 fps, microphone + system audio enabled**. README needs a sync pass.

#### [M3] `.csproj.user` committed

`KeepSessionAlive.csproj.user` is Visual Studio's per-user project settings file (local paths, debug settings). It should be in `.gitignore`.

---

### 🔵 LOW

#### [L1] `GetIdleTimeMs()` — `Environment.TickCount` sign cast note

`(uint)Environment.TickCount` is a well-known correct pattern for wrapping subtraction, but `Environment.TickCount` is a 32-bit signed int that wraps after ~24.9 days. On sessions running longer than that, the idle detection may misfire briefly once per wrap. `Environment.TickCount64` (available on .NET 5+, not .NET Framework 4.8) would eliminate this entirely, but since the project targets .NET 4.8 the current approach is the standard workaround.

#### [L2] `PrivateFontCollection` never disposed

`_faFonts` (a `PrivateFontCollection`) is static and held for the lifetime of the process. This is intentional, but it should be noted that it cannot be disposed while the font is still in use by any control.

---

## Development Plan — Next Steps

Priority order based on risk and effort:

| # | Task | Issue | Effort |
|---|------|-------|--------|
| 1 | Fix GDI handle leak in `BuildTrayIcon()` | C1 | 10 min |
| 2 | Remove or wire `runQuery()` | C2 | 5 min |
| 3 | Make `StartRegionCapture()` async | H1 | 30 min |
| 4 | Add `.pfx` and `.csproj.user` to `.gitignore` | H2, M3 | 5 min |
| 5 | Add `_appRowIndex` dictionary | M1 | 20 min |
| 6 | Sync README recording settings | M2 | 10 min |

### Feature Backlog (from README + discussion)

- CPU / RAM usage indicator in status strip
- Always-on-top window toggle
- Quick timer / Pomodoro mode
- `runQuery()` generalized into configurable SQL snippet runner with coordinate presets
