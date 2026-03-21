# KeepSessionAlive

A WinForms (.NET Framework 4.8) desktop productivity utility with a dark theme (orange accent on dark gray/black). Borderless window with custom title bar.

## Project Structure

```
KeepSessionAlive/
  MainForm.cs              - All main logic (single partial class)
  MainForm.Designer.cs     - UI layout (hand-coded, no VS designer)
  PptxExporter.cs          - PowerPoint export (separated to avoid namespace conflicts)
  Program.cs               - Entry point
  Resources/
    Font Awesome 7 Free-Solid-900.otf   - Embedded resource for icons
  packages.config          - NuGet references
  KeepSessionAlive.csproj  - Project file
```

## NuGet Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `ScreenRecorderLib` | 6.6.0 | Screen + mic recording to MP4 (uses Windows Media Foundation, no FFmpeg) |
| `DocumentFormat.OpenXml` | 2.20.0 | PowerPoint (.pptx) creation without Office installed |
| `InputSimulator` | 1.0.4.0 | Keyboard/mouse simulation |

**Important**: ScreenRecorderLib 6.6.0 requires .NET Framework 4.8 (not 4.7.2). The project was upgraded to 4.8.

## Features

### 1. Keep Online (Mouse Jiggler)
- **Status strip button**: FontAwesome power icon (`\uf011`)
- **Off state**: Gray icon. **On state**: LimeGreen icon
- After 5 minutes idle, simulates activity (right-click at center, dismiss with left-click)
- Checks idle time every 10 seconds
- Toggle via status strip or tray menu

### 2. App Time Tracker (DataGridView)
- Tracks which application is in the foreground each second
- Columns: Application name, Time (h:mm:ss), Bar chart
- **Bar chart column** (`colBar`): Custom-painted via `CellPainting` event. Orange horizontal bar proportional to each app's time vs the max. Refreshes every 20 seconds, only when window is visible
- **Browser detection**: For Chrome/Edge/Firefox, reads the address bar via UI Automation to show the domain (e.g., "Chrome: github.com")
- Row selection is invisible (selection colors match row colors) so bar chart is always visible

### 3. Working Time / Idle Time Display
- Two large labels (36pt bold) showing cumulative work and idle time
- Working time: LimeGreen. Idle time: Goldenrod
- Idle starts counting after 10 seconds of no input

### 4. Screen Recording
- **Status strip button**: FontAwesome video camera icon (`\uf03d`), positioned on the right end via a spacer with `Spring = true`
- **3-second countdown overlay**: Full-screen semi-transparent black form with large white countdown numbers (180pt)
- Records using `ScreenRecorderLib.Recorder` with these settings:
  - Resolution: half of primary screen (e.g., 960x540 for 1080p)
  - Bitrate: 2 Mbps CBR (~CRF 26-28 equivalent)
  - Framerate: 15 fps, variable (only encodes when screen changes)
  - Audio: microphone enabled, system audio disabled
- During recording: button changes to stop icon (`\uf04d`), app minimizes to tray
- On stop: shows SaveFileDialog. If cancelled, recording is discarded (not frozen)
- Stop recording also available via tray right-click menu
- Temp file stored in `%TEMP%`, moved to user-selected location on save

### 5. Screen Capture to PowerPoint
- **Camera button** (`\uf030`): Click to capture a screen region
  - App minimizes, takes screenshot of all screens, shows full-screen overlay with dimmed background
  - User drags to select region (bright/undimmed selection area, orange border)
  - Esc to cancel
  - Screenshot taken BEFORE overlay form is created (prevents white flash)
  - DoubleBuffered enabled on overlay to prevent flicker
- **PowerPoint export button** (`\uf1c4`): Appears after first capture
  - Shows capture count on camera button: `camera_icon 3`
  - Creates .pptx with one screenshot per slide (16:9 widescreen)
  - Images fit-to-slide preserving aspect ratio, centered
  - Uses `PptxExporter.cs` (separated to avoid `ImageFormat` namespace conflict between `System.Drawing.Imaging` and `ScreenRecorderLib`)
  - After export, captures are cleared from memory

### 6. Snap Windows to Grid
- **Status strip button**: FontAwesome TV/monitor icon (`\uf26c`)
- Finds top visible windows (by z-order) excluding KeepSessionAlive itself
- Snaps up to 4 windows per screen into quadrants (top-left, top-right, bottom-left, bottom-right)
- Multi-monitor: supports up to 8 windows across 2 screens (4 per screen)
- Skips tool windows, cloaked UWP windows, owned windows, and windows with no title
- Restores minimized windows before snapping

### 7. Lock Screen
- **Status strip button**: FontAwesome lock icon (`\uf023`)
- Calls `LockWorkStation()` Win32 API

### 8. Log Panel
- **Status strip button**: FontAwesome document icon (`\uf15c`)
- Toggles a multiline TextBox below the time labels
- Form height adjusts by 116px when shown/hidden
- Thread-safe logging via `AppendTextBox()` with `InvokeRequired` check

## UI Architecture

### Dark Theme
Applied in `ApplyDarkTheme()`:
- Background: `rgb(28, 28, 28)`
- Surface: `rgb(45, 45, 45)`
- Border: `rgb(70, 70, 70)`
- Accent: `rgb(255, 140, 0)` (orange)
- Text: `rgb(220, 220, 220)`
- Title bar: `rgb(20, 20, 20)`

### Custom Title Bar
- Borderless form (`FormBorderStyle.None`) with a Panel docked to top (35px)
- Draggable via `MouseDown`/`MouseMove`/`MouseUp` handlers on panel and label
- Custom minimize (`\u2500`) and close (`\u2715`) buttons
- Close button hover: red (`rgb(180, 30, 30)`)

### Status Strip (40px, bottom-docked)
All buttons are `ToolStripStatusLabel` with `IsLink = true`, `LinkBehavior = NeverUnderline`.
Font: FontAwesome 14pt loaded from embedded resource.

| Position | Icon | Codepoint | Function |
|----------|------|-----------|----------|
| Left 1 | Power | `\uf011` | Keep Online toggle |
| Left 2 | Document | `\uf15c` | Toggle log |
| Left 3 | Lock | `\uf023` | Lock screen |
| Left 4 | Monitor | `\uf26c` | Snap windows |
| Left 5 | Camera | `\uf030` | Capture region |
| Left 6 | PPT file | `\uf1c4` | Export to PowerPoint (hidden until captures exist) |
| Spacer | (Spring) | | Pushes record button right |
| Right | Video | `\uf03d` | Start/stop recording |

`ShowItemToolTips = true` on the StatusStrip (default is false for StatusStrip, unlike ToolStrip).
`AutoSize = false` to force 40px height.

### FontAwesome Loading
- `.otf` file embedded as project resource (`Build Action: Embedded Resource`)
- Loaded at startup via `PrivateFontCollection` + `AddFontMemResourceEx` (GDI registration required for WinForms controls)
- Resource name: `"KeepSessionAlive.Resources.Font Awesome 7 Free-Solid-900.otf"` (note: spaces in filename are preserved, not replaced with underscores)
- `FaFont(float size)` helper returns a `Font` instance

### System Tray
- Orange circle icon built programmatically (`BuildTrayIcon()`)
- Minimizes to tray on window minimize
- Double-click tray icon to restore
- Right-click context menu mirrors all status strip features:
  - Keep Online, Start/Stop Recording, Snap Windows, Capture Region, Export to PowerPoint, Lock Screen, Restore, Exit
- Dark-themed context menu (surface background, light text)

## Video Recording Quality Settings

```csharp
OutputFrameSize = new ScreenSize(Width / 2, Height / 2)  // Half resolution
Bitrate = 2000 * 1000       // 2 Mbps
Framerate = 15              // 15 fps
IsFixedFramerate = false    // Variable framerate (saves space)
BitrateMode = H264BitrateControlMode.CBR  // Constant bit rate
```

To switch to quality-based encoding:
```csharp
BitrateMode = H264BitrateControlMode.Quality,
Quality = 70  // 0-100, roughly CRF 23 equivalent
```

Estimated file size: ~15 MB/min at current settings.

## Known Design Decisions

- **No FFmpeg**: Company computer restrictions. Uses ScreenRecorderLib (Windows Media Foundation) instead
- **Single file preference**: User copies code between computers. Features stay in MainForm.cs where possible. PptxExporter.cs was separated only to resolve namespace conflicts
- **FontAwesome over emoji**: Emoji rendering was inconsistent (wrong colors, too small, not clickable-looking). FA icons render reliably at any size with controllable colors
- **No row selection highlighting**: DataGridView selection colors set to match normal row colors so the bar chart column is always visible
- **Bar chart refresh interval**: 20 seconds (not every second) to minimize CPU usage. Only refreshes when window is visible

## Building

1. Open in Visual Studio 2022
2. Ensure NuGet packages are restored (ScreenRecorderLib 6.6.0, DocumentFormat.OpenXml 2.20.0, InputSimulator 1.0.4.0)
3. Target: .NET Framework 4.8, x64
4. The FontAwesome `.otf` file must be in `Resources/` with Build Action set to `Embedded Resource`

## Future Ideas Discussed

- Clipboard history (but Windows Win+V already does this)
- Quick timer / Pomodoro
- CPU/RAM usage indicator in status strip
- Always-on-top window toggle
- AD (Active Directory) group/user lookup (user has existing code for this, will share later)
