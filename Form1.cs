using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Forms;
using WindowsInput;

namespace KeepSessionAlive
{
    public partial class Form1 : Form
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

        public Form1()
        {
            InitializeComponent();

            notifyIcon1.Icon = BuildTrayIcon();

            _idleDisplayTimer = new System.Windows.Forms.Timer();
            _idleDisplayTimer.Interval = 1000;
            _idleDisplayTimer.Tick += IdleDisplayTimer_Tick;
            _idleDisplayTimer.Start();
        }

        // Programmatically build a small green circle icon for the tray
        private static System.Drawing.Icon BuildTrayIcon()
        {
            using (var bmp = new System.Drawing.Bitmap(16, 16))
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.Clear(System.Drawing.Color.Transparent);
                g.FillEllipse(System.Drawing.Brushes.Green,  1, 1, 13, 13);
                g.DrawEllipse(new System.Drawing.Pen(System.Drawing.Color.DarkGreen, 1), 1, 1, 13, 13);
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

        private void buttonLock_Click(object sender, EventArgs e) => LockWorkStation();

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

        // --- Log toggle button ---
        private void buttonLog_Click(object sender, EventArgs e)
        {
            bool show = !textBox1.Visible;
            textBox1.Visible = show;

            int delta = show ? LogAreaHeight : -LogAreaHeight;
            dataGridView1.Top  += delta;
            labelWorkTitle.Top += delta;
            labelWorkTime.Top  += delta;
            labelIdleTitle.Top += delta;
            labelIdleTime.Top  += delta;
            this.Height        += delta;

            buttonLog.Text = show ? "Hide Log" : "Log";
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

        // --- Start / Stop button ---
        private async void button1_Click(object sender, EventArgs e)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts = null;
                button1.Text = "Always online : Off";
                AppendTextBox($"{DateTime.Now:HH:mm:ss} - Stopped.\r\n");
                return;
            }

            _cts = new CancellationTokenSource();
            button1.Text = "Always online : On";
            AppendTextBox($"{DateTime.Now:HH:mm:ss} - Started. Waiting for {IdleThresholdMs / 60000} min idle...\r\n");

            var token = _cts.Token;
            await Task.Run(() => RunLoop(token));

            if (!token.IsCancellationRequested)
            {
                Invoke(new Action(() =>
                {
                    button1.Text = "Always online : Off";
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
