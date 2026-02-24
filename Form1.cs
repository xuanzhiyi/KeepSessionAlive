using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;

namespace KeepSessionAlive
{
    public partial class Form1 : Form
    {
        // --- P/Invoke ---
        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [DllImport("user32.dll")]
        static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        internal static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

        // --- Mouse constants ---
        private const int MOUSEEVENTF_MOVE       = 0x0001;
        private const int MOUSEEVENTF_LEFTDOWN   = 0x0002;
        private const int MOUSEEVENTF_LEFTUP     = 0x0004;
        private const int MOUSEEVENTF_RIGHTDOWN  = 0x0008;
        private const int MOUSEEVENTF_RIGHTUP    = 0x0010;
        private const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const int MOUSEEVENTF_MIDDLEUP   = 0x0040;
        private const int MOUSEEVENTF_ABSOLUTE   = 0x8000;

        // 5 minutes of idle before triggering; check every 10 seconds
        private const int IdleThresholdMs = 5 * 60 * 1000;
        private const int CheckIntervalMs = 10_000;

        // --- Structs ---
        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

#pragma warning disable 649
        internal struct INPUT
        {
            public UInt32 Type;
            public MOUSEKEYBDHARDWAREINPUT Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public MOUSEINPUT Mouse;
        }

        internal struct MOUSEINPUT
        {
            public Int32 X;
            public Int32 Y;
            public UInt32 MouseData;
            public UInt32 Flags;
            public UInt32 Time;
            public IntPtr ExtraInfo;
        }
#pragma warning restore 649

        // --- State ---
        private CancellationTokenSource _cts;

        public Form1()
        {
            InitializeComponent();
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
                button1.Text = "Start";
                AppendTextBox($"{DateTime.Now:HH:mm:ss} - Stopped.\r\n");
                return;
            }

            _cts = new CancellationTokenSource();
            button1.Text = "Stop";
            AppendTextBox($"{DateTime.Now:HH:mm:ss} - Started. Waiting for {IdleThresholdMs / 60000} min idle...\r\n");

            var token = _cts.Token;
            await Task.Run(() => RunLoop(token));

            // Reached only if RunLoop exits naturally (shouldn't happen, but be safe)
            if (!token.IsCancellationRequested)
            {
                Invoke(new Action(() =>
                {
                    button1.Text = "Start";
                    _cts = null;
                }));
            }
        }

        // --- Main loop: wait for idle, then simulate activity ---
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

                // Sleep in small chunks so cancellation is responsive
                for (int i = 0; i < CheckIntervalMs / 500 && !ct.IsCancellationRequested; i++)
                    Thread.Sleep(500);
            }
        }

        // --- Safe activity: right-click at center, move left, left-click to dismiss ---
        private void PerformActivity()
        {
            int cx = Screen.PrimaryScreen.Bounds.Width  / 2;
            int cy = Screen.PrimaryScreen.Bounds.Height / 2;

            // Move to screen center
            Cursor.Position = new Point(cx, cy);
            Thread.Sleep(400);

            // Right-click: opens a context menu without executing anything
            RightClick();
            Thread.Sleep(600);

            // Move left 150px — context menus open to the right/below, so this
            // lands clearly outside the menu
            Cursor.Position = new Point(cx - 150, cy);
            Thread.Sleep(400);

            // Left-click: dismisses the context menu harmlessly
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

        // --- Open Citrix (unchanged) ---
        private void button2_Click(object sender, EventArgs e)
        {
            var psi = new ProcessStartInfo
            {
                FileName = @"https://csgsf.oneadr.net/Citrix/VipProd_ExternalStoreWeb/",
                UseShellExecute = true,
            };
            Process p = null;
            try
            {
                p = Process.Start(psi);
                p.WaitForInputIdle();
                var window = p.MainWindowHandle;
                Thread.Sleep(1000);
                ClickOnPoint(window, new Point(300, 300));
                p.WaitForExit();
            }
            catch { }
            try
            {
                if (p.HasExited) { }
            }
            catch (InvalidOperationException) { }
        }

        public static void ClickOnPoint(IntPtr wndHandle, Point clientPoint)
        {
            ClientToScreen(wndHandle, ref clientPoint);
            Cursor.Position = new Point(clientPoint.X, clientPoint.Y);

            var inputMouseDown = new INPUT();
            inputMouseDown.Type = 0;
            inputMouseDown.Data.Mouse.Flags = 0x0002;

            var inputMouseUp = new INPUT();
            inputMouseUp.Type = 0;
            inputMouseUp.Data.Mouse.Flags = 0x0004;

            var inputs = new INPUT[] { inputMouseDown, inputMouseUp };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
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
