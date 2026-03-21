using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Forms;
using ScreenRecorderLib;
using WindowsInput;

namespace KeepSessionAlive
{
    public partial class MainForm : Form
    {
        // --- P/Invoke ---
        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [DllImport("user32.dll")]
        static extern bool LockWorkStation();

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        // --- Mouse constants ---
        private const int MOUSEEVENTF_MOVE      = 0x0001;
        private const int MOUSEEVENTF_LEFTDOWN  = 0x0002;
        private const int MOUSEEVENTF_LEFTUP    = 0x0004;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const int MOUSEEVENTF_RIGHTUP   = 0x0010;
        private const int MOUSEEVENTF_ABSOLUTE  = 0x8000;

        // 5 minutes of idle before triggering keep-alive; check every 10 seconds
        private const int IdleThresholdMs        = 5 * 60 * 1000;
        private const int CheckIntervalMs        = 10_000;

        // Idle display starts after 10 s; app time only counted when active
        private const int IdleDisplayThresholdMs = 10_000;

        // Height added to the form when the log panel is visible
        private const int LogAreaHeight = 116; // textBox1 height (110) + gap (6)

        // --- Structs ---
        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        // --- State ---
        private CancellationTokenSource _cts;
        private System.Windows.Forms.Timer _idleDisplayTimer;
        private long _totalWorkSeconds = 0;
        private long _totalIdleSeconds = 0;

        // App tracking: name -> accumulated seconds, name -> grid row index
        private readonly Dictionary<string, long> _appSeconds = new Dictionary<string, long>();
        private readonly Dictionary<string, int>  _appRowMap  = new Dictionary<string, int>();

        // --- Recording state ---
        private Recorder _recorder;
        private string   _tempVideoPath;
        private bool     _isRecording;

        // --- FontAwesome ---
        private static PrivateFontCollection _faFonts;
        private static FontFamily _faFamily;

        [DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont,
            IntPtr pdv, [In] ref uint pcFonts);

        private static void LoadFontAwesome()
        {
            if (_faFonts != null) return;
            _faFonts = new PrivateFontCollection();
            var asm = Assembly.GetExecutingAssembly();
            var resName = "KeepSessionAlive.Resources.Font Awesome 7 Free-Solid-900.otf";
            using (var stream = asm.GetManifestResourceStream(resName))
            {
                if (stream == null) return;
                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                IntPtr ptr = Marshal.AllocCoTaskMem(data.Length);
                Marshal.Copy(data, 0, ptr, data.Length);

                // Register with GDI so WinForms controls render correctly
                uint fontCount = 0;
                AddFontMemResourceEx(ptr, (uint)data.Length, IntPtr.Zero, ref fontCount);

                // Also register with GDI+ for FontFamily access
                _faFonts.AddMemoryFont(ptr, data.Length);
            }
            _faFamily = _faFonts.Families[0];
        }

        private static Font FaFont(float size)
        {
            return new Font(_faFamily, size, FontStyle.Regular, GraphicsUnit.Point);
        }

        public MainForm()
        {
            LoadFontAwesome();
            InitializeComponent();

            notifyIcon1.Icon = BuildTrayIcon();
            ApplyDarkTheme();

            _idleDisplayTimer = new System.Windows.Forms.Timer();
            _idleDisplayTimer.Interval = 1000;
            _idleDisplayTimer.Tick += IdleDisplayTimer_Tick;
            _idleDisplayTimer.Start();
        }

        private void ApplyDarkTheme()
        {
            var bg      = System.Drawing.Color.FromArgb(28, 28, 28);
            var surface = System.Drawing.Color.FromArgb(45, 45, 45);
            var border  = System.Drawing.Color.FromArgb(70, 70, 70);
            var orange  = System.Drawing.Color.FromArgb(255, 140, 0);
            var text    = System.Drawing.Color.FromArgb(220, 220, 220);
            var hover   = System.Drawing.Color.FromArgb(65, 65, 65);

            // Form
            this.BackColor = bg;
            this.ForeColor = text;

            // Status strip
            statusStrip1.BackColor = System.Drawing.Color.FromArgb(20, 20, 20);
            statusStrip1.ForeColor = text;
            statusOnline.LinkColor         = System.Drawing.Color.Gray;
            statusOnline.ActiveLinkColor   = System.Drawing.Color.White;
            statusLog.ForeColor        = orange;
            statusLog.LinkColor        = orange;
            statusLog.ActiveLinkColor  = System.Drawing.Color.White;
            statusLock.ForeColor       = orange;
            statusLock.LinkColor       = orange;
            statusLock.ActiveLinkColor = System.Drawing.Color.White;
            statusRecord.ForeColor       = System.Drawing.Color.IndianRed;
            statusRecord.LinkColor       = System.Drawing.Color.IndianRed;
            statusRecord.ActiveLinkColor = System.Drawing.Color.Red;

            // Log text box
            textBox1.BackColor = surface;
            textBox1.ForeColor = text;
            textBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            // DataGridView
            dataGridView1.BackgroundColor = bg;
            dataGridView1.GridColor = border;
            dataGridView1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dataGridView1.EnableHeadersVisualStyles = false;

            dataGridView1.DefaultCellStyle.BackColor          = surface;
            dataGridView1.DefaultCellStyle.ForeColor          = text;
            dataGridView1.DefaultCellStyle.SelectionBackColor = orange;
            dataGridView1.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black;

            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(38, 38, 38);
            dataGridView1.AlternatingRowsDefaultCellStyle.ForeColor = text;

            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor          = System.Drawing.Color.FromArgb(35, 35, 35);
            dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor          = orange;
            dataGridView1.ColumnHeadersDefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(35, 35, 35);
            dataGridView1.ColumnHeadersDefaultCellStyle.SelectionForeColor = orange;
            dataGridView1.ColumnHeadersDefaultCellStyle.Font =
                new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);

            // Section title labels
            labelWorkTitle.ForeColor = orange;
            labelIdleTitle.ForeColor = orange;

            // Working time: brighter green for dark background
            labelWorkTime.ForeColor = System.Drawing.Color.LimeGreen;
            // Idle time: goldenrod already visible on dark bg — no change needed

            // Tray context menu
            trayContextMenu.BackColor = surface;
            trayContextMenu.ForeColor = text;
            trayMenuRestore.BackColor = surface;
            trayMenuRestore.ForeColor = text;
            trayMenuExit.BackColor    = surface;
            trayMenuExit.ForeColor    = text;
        }

        // ── Custom title bar drag ──────────────────────────────────────────────
        private System.Drawing.Point _dragStart;
        private bool _dragging;

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _dragging  = true;
                _dragStart = e.Location;
            }
        }

        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dragging)
                this.Location = new System.Drawing.Point(
                    this.Left + e.X - _dragStart.X,
                    this.Top  + e.Y - _dragStart.Y);
        }

        private void TitleBar_MouseUp(object sender, MouseEventArgs e)
        {
            _dragging = false;
        }

        private void BtnClose_Click(object sender, EventArgs e)    => Application.Exit();
        private void BtnMinimize_Click(object sender, EventArgs e) => this.WindowState = FormWindowState.Minimized;

        // Programmatically build a small green circle icon for the tray
        private static System.Drawing.Icon BuildTrayIcon()
        {
            using (var bmp = new System.Drawing.Bitmap(16, 16))
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.Clear(System.Drawing.Color.Transparent);
                g.FillEllipse(System.Drawing.Brushes.Orange,  1, 1, 13, 13);
                g.DrawEllipse(new System.Drawing.Pen(System.Drawing.Color.Black, 2), 1, 1, 13, 13);
                IntPtr hIcon = bmp.GetHicon();
                return System.Drawing.Icon.FromHandle(hIcon);
            }
        }

        // --- Tray: minimize to tray on window minimize ---
        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon1.ShowBalloonTip(1000, "Keep Session Alive", "Running in the background.", System.Windows.Forms.ToolTipIcon.Info);
            }
        }

        private void NotifyIcon1_DoubleClick(object sender, EventArgs e) => RestoreFromTray();

        private void TrayMenuRestore_Click(object sender, EventArgs e) => RestoreFromTray();

        private void TrayMenuExit_Click(object sender, EventArgs e)
        {
            notifyIcon1.Visible = false;
            Application.Exit();
        }

        private void statusLock_Click(object sender, EventArgs e) => LockWorkStation();

        private void RestoreFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        // --- Every-second timer: work/idle counters + app tracker ---
        private void IdleDisplayTimer_Tick(object sender, EventArgs e)
        {
            uint idleMs = GetIdleTimeMs();

            if (idleMs >= IdleDisplayThresholdMs)
            {
                _totalIdleSeconds++;
                labelIdleTime.Text = FormatTime(_totalIdleSeconds);
            }
            else
            {
                _totalWorkSeconds++;
                labelWorkTime.Text = FormatTime(_totalWorkSeconds);

                string app = GetForegroundAppName();
                if (app != null)
                    RecordAppSecond(app);
            }
        }

        private static string FormatTime(long totalSeconds)
        {
            long h = totalSeconds / 3600;
            long m = (totalSeconds % 3600) / 60;
            long s = totalSeconds % 60;
            return $"{h}:{m:D2}:{s:D2}";
        }

        // Browsers whose active tab URL we can read via UI Automation
        private static readonly HashSet<string> _browserProcesses =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "chrome", "msedge", "firefox", "opera", "brave", "vivaldi" };

        // --- Get the display name for the foreground window ---
        // Returns "Chrome: google.com" for browsers, "Notepad" for others.
        private static string GetForegroundAppName()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return null;
                GetWindowThreadProcessId(hwnd, out uint pid);
                string procName = Process.GetProcessById((int)pid).ProcessName;
                string friendly = char.ToUpper(procName[0]) + procName.Substring(1);

                if (_browserProcesses.Contains(procName))
                {
                    string domain = GetBrowserDomain(hwnd);
                    if (domain != null)
                        return $"{friendly}: {domain}";
                }
                return friendly;
            }
            catch { return null; }
        }

        // --- Read address bar via UI Automation and return the host (domain) ---
        private static string GetBrowserDomain(IntPtr hwnd)
        {
            try
            {
                var root = AutomationElement.FromHandle(hwnd);

                // Chrome & Edge expose the address bar as an Edit named "Address and search bar"
                // Firefox uses a similar pattern but with a different name
                var editCondition = new PropertyCondition(
                    AutomationElement.ControlTypeProperty, ControlType.Edit);
                var edits = root.FindAll(TreeScope.Descendants, editCondition);

                foreach (AutomationElement el in edits)
                {
                    try
                    {
                        string name = el.Current.Name ?? "";
                        // Match Chrome/Edge and Firefox address bars
                        if (!name.Contains("ddress") && !name.Contains("earch")) continue;

                        var vp = el.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern;
                        if (vp == null) continue;

                        string url = vp.Current.Value;
                        return ExtractDomain(url);
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        // --- Strip a URL down to its bare hostname (no www.) ---
        private static string ExtractDomain(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            try
            {
                // Not a navigable URL (e.g. a search query or internal page)
                if (url.StartsWith("chrome://") || url.StartsWith("about:") ||
                    url.StartsWith("edge://")   || url.StartsWith("moz-extension://"))
                    return url;

                if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    url = "https://" + url;

                string host = new Uri(url).Host;
                if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
                    host = host.Substring(4);
                return host;
            }
            catch { return null; }
        }

        // --- Add one second to the given app's row in the grid ---
        private void RecordAppSecond(string appName)
        {
            if (!_appSeconds.ContainsKey(appName))
            {
                _appSeconds[appName] = 0;
                int idx = dataGridView1.Rows.Add(appName, "0:00:00");
                _appRowMap[appName] = idx;
            }

            _appSeconds[appName]++;
            long total = _appSeconds[appName];
            long h = total / 3600;
            long m = (total % 3600) / 60;
            long s = total % 60;
            dataGridView1.Rows[_appRowMap[appName]].Cells["colTime"].Value = $"{h}:{m:D2}:{s:D2}";
        }

        // --- Log toggle (status strip) ---
        private void statusLog_Click(object sender, EventArgs e)
        {
            bool show = !textBox1.Visible;
            textBox1.Visible = show;
            this.Height += show ? LogAreaHeight : -LogAreaHeight;
        }

        // --- Idle time helper ---
        private static uint GetIdleTimeMs()
        {
            var info = new LASTINPUTINFO();
            info.cbSize = (uint)Marshal.SizeOf(info);
            GetLastInputInfo(ref info);
            return (uint)Environment.TickCount - info.dwTime;
        }

        // --- Mouse helpers ---
        public static void MoveM(int xDelta, int yDelta)
        {
            mouse_event(MOUSEEVENTF_MOVE, xDelta, yDelta, 0, 0);
        }

        public static void MoveTo(int x, int y)
        {
            mouse_event(MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE, x, y, 0, 0);
        }

        public static void LeftClick()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP,   Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
        }

        public static void LeftDown()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
        }

        public static void LeftUp()
        {
            mouse_event(MOUSEEVENTF_LEFTUP, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
        }

        public static void RightClick()
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
            mouse_event(MOUSEEVENTF_RIGHTUP,   Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
        }

        public static void RightDown()
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
        }

        public static void RightUp()
        {
            mouse_event(MOUSEEVENTF_RIGHTUP, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
        }

        // --- Start / Stop (status strip) ---
        private async void statusOnline_Click(object sender, EventArgs e)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts = null;
                statusOnline.ForeColor = System.Drawing.Color.Gray;
                statusOnline.LinkColor = System.Drawing.Color.Gray;
                statusOnline.ToolTipText = "Keep Online: Off";
                AppendTextBox($"{DateTime.Now:HH:mm:ss} - Stopped.\r\n");
                return;
            }

            _cts = new CancellationTokenSource();
            statusOnline.ForeColor = System.Drawing.Color.LimeGreen;
            statusOnline.LinkColor = System.Drawing.Color.LimeGreen;
            statusOnline.ToolTipText = "Keep Online: On";
            AppendTextBox($"{DateTime.Now:HH:mm:ss} - Started. Waiting for {IdleThresholdMs / 60000} min idle...\r\n");

            var token = _cts.Token;
            await Task.Run(() => RunLoop(token));

            if (!token.IsCancellationRequested)
            {
                Invoke(new Action(() =>
                {
                    statusOnline.ForeColor = System.Drawing.Color.Gray;
                    statusOnline.LinkColor = System.Drawing.Color.Gray;
                    statusOnline.ToolTipText = "Keep Online: Off";
                    _cts = null;
                }));
            }
        }

        // --- Main loop: wait for idle threshold, then simulate activity ---
        private void RunLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                uint idleMs = GetIdleTimeMs();

                if (idleMs >= IdleThresholdMs)
                {
                    AppendTextBox($"{DateTime.Now:HH:mm:ss} - Idle for {idleMs / 1000}s, simulating activity...\r\n");
                    PerformActivity();
                    AppendTextBox($"{DateTime.Now:HH:mm:ss} - Done. Resuming idle watch...\r\n");
                }

                for (int i = 0; i < CheckIntervalMs / 500 && !ct.IsCancellationRequested; i++)
                    Thread.Sleep(500);
            }
        }

        // --- Safe activity: right-click at center, dismiss with left-click ---
        private void PerformActivity()
        {
            int cx = Screen.PrimaryScreen.Bounds.Width  / 2;
            int cy = Screen.PrimaryScreen.Bounds.Height / 2;

            Cursor.Position = new System.Drawing.Point(cx, cy);
            Thread.Sleep(400);

            RightClick();
            Thread.Sleep(600);

            Cursor.Position = new System.Drawing.Point(cx - 150, cy);
            Thread.Sleep(400);

            LeftClick();
            Thread.Sleep(400);
        }

        // --- Thread-safe log ---
        public void AppendTextBox(string value)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(AppendTextBox), new object[] { value });
                return;
            }
            textBox1.AppendText(value);
        }

        // ── Screen recording (ScreenRecorderLib) ─────────────────────────────

        private async void statusRecord_Click(object sender, EventArgs e)
        {
            if (_isRecording)
            {
                statusRecord.Enabled = false;
                _recorder?.Stop();
                return;
            }

            statusRecord.Enabled = false;
            await ShowCountdownAsync();
            StartRecording();
            statusRecord.Text = "\uf04d";
            statusRecord.ToolTipText = "Stop Recording";
            statusRecord.Enabled = true;
            _isRecording = true;

            // Minimize to tray so the app window isn't in the recording
            this.WindowState = FormWindowState.Minimized;
        }

        // ── Inline countdown overlay ────────────────────────────────────────
        private async Task ShowCountdownAsync()
        {
            using (var overlay = new Form())
            {
                overlay.FormBorderStyle = FormBorderStyle.None;
                overlay.WindowState     = FormWindowState.Maximized;
                overlay.TopMost         = true;
                overlay.BackColor       = Color.Black;
                overlay.Opacity         = 0.65;
                overlay.ShowInTaskbar   = false;
                overlay.StartPosition   = FormStartPosition.CenterScreen;

                var lbl = new Label
                {
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font      = new Font("Segoe UI", 180F, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,
                };
                overlay.Controls.Add(lbl);
                overlay.Show();

                for (int i = 3; i >= 1; i--)
                {
                    lbl.Text = i.ToString();
                    lbl.Refresh();
                    await Task.Delay(1000);
                }
            }
        }

        private void StartRecording()
        {
            _tempVideoPath = Path.Combine(
                Path.GetTempPath(),
                $"recording_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");

            var opts = new RecorderOptions
            {
                OutputOptions = new OutputOptions
                {
                    RecorderMode   = RecorderMode.Video,
                    OutputFrameSize = new ScreenSize(
                        Screen.PrimaryScreen.Bounds.Width / 2,
                        Screen.PrimaryScreen.Bounds.Height / 2),
                },
                VideoEncoderOptions = new VideoEncoderOptions
                {
                    Bitrate   = 2000 * 1000,
                    Framerate = 15,
                    IsFixedFramerate = false,
                    Encoder   = new H264VideoEncoder
                    {
                        BitrateMode = H264BitrateControlMode.CBR,
                    },
                },
                AudioOptions = new AudioOptions
                {
                    IsAudioEnabled         = true,
                    IsInputDeviceEnabled   = true,   // microphone
                    IsOutputDeviceEnabled  = false,   // no system audio
                },
            };

            _recorder = Recorder.CreateRecorder(opts);
            _recorder.OnRecordingComplete += Recorder_OnRecordingComplete;
            _recorder.OnRecordingFailed   += Recorder_OnRecordingFailed;
            _recorder.Record(_tempVideoPath);

            AppendTextBox($"{DateTime.Now:HH:mm:ss} - Recording started.\r\n");
        }

        private void Recorder_OnRecordingComplete(object sender, RecordingCompleteEventArgs e)
        {
            _isRecording = false;
            BeginInvoke(new Action(() =>
            {
                AppendTextBox($"{DateTime.Now:HH:mm:ss} - Recording stopped.\r\n");
                statusRecord.Text = "\uf03d";
                statusRecord.ToolTipText = "Start Recording";
                statusRecord.Enabled = true;
                CleanupRecorder();
                SaveRecording();
            }));
        }

        private void Recorder_OnRecordingFailed(object sender, RecordingFailedEventArgs e)
        {
            _isRecording = false;
            BeginInvoke(new Action(() =>
            {
                AppendTextBox($"{DateTime.Now:HH:mm:ss} - Recording failed: {e.Error}\r\n");
                statusRecord.Text = "\uf03d";
                statusRecord.ToolTipText = "Start Recording";
                statusRecord.Enabled = true;
                CleanupRecorder();
            }));
        }

        private void CleanupRecorder()
        {
            if (_recorder != null)
            {
                _recorder.OnRecordingComplete -= Recorder_OnRecordingComplete;
                _recorder.OnRecordingFailed   -= Recorder_OnRecordingFailed;
                _recorder.Dispose();
                _recorder = null;
            }
        }

        private void SaveRecording()
        {
            if (string.IsNullOrEmpty(_tempVideoPath) || !File.Exists(_tempVideoPath))
            {
                AppendTextBox($"{DateTime.Now:HH:mm:ss} - Recording file not found.\r\n");
                return;
            }
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title      = "Save Recording";
                dlg.Filter     = "MP4 Video|*.mp4";
                dlg.FileName   = $"Recording_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.mp4";
                dlg.DefaultExt = "mp4";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        if (File.Exists(dlg.FileName)) File.Delete(dlg.FileName);
                        File.Move(_tempVideoPath, dlg.FileName);
                        AppendTextBox($"{DateTime.Now:HH:mm:ss} - Saved to {dlg.FileName}\r\n");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to save: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    try { File.Delete(_tempVideoPath); } catch { }
                    AppendTextBox($"{DateTime.Now:HH:mm:ss} - Recording discarded.\r\n");
                }
            }
            _tempVideoPath = null;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isRecording)
            {
                try { _recorder?.Stop(); } catch { }
                CleanupRecorder();
                try { if (_tempVideoPath != null) File.Delete(_tempVideoPath); } catch { }
            }
            base.OnFormClosing(e);
        }

        private void runQuery()
        {
            InputSimulator sim = new InputSimulator();

            sim.Mouse.Sleep(5000).MoveMouseTo(500, 2500)
                .Sleep(1000).LeftButtonClick()
                .Sleep(1000).MoveMouseTo(500, 7000)
                .Sleep(1000).MoveMouseTo(13500, 7000).MoveMouseTo(13500, 9500).Sleep(1000).LeftButtonClick();

            sim.Keyboard.Sleep(7000).TextEntry("select getdate()");

            sim.Mouse.Sleep(1000).MoveMouseTo(25000, 29000)
                .Sleep(1000).RightButtonClick()
                .Sleep(1000).MoveMouseTo(26000, 40000)
                .Sleep(1000).LeftButtonClick();

            AppendTextBox($"{DateTime.Now.ToLocalTime()} run query\r\n");
        }
    }
}
