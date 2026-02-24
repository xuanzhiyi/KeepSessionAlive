using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;

namespace KeepSessionAlive
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);
        private const int MOUSEEVENTF_MOVE = 0x0001;
        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const int MOUSEEVENTF_LEFTUP = 0x0004;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const int MOUSEEVENTF_RIGHTUP = 0x0010;
        private const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const int MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const int MOUSEEVENTF_ABSOLUTE = 0x8000;

        private long count = 0;

        BackgroundWorker bgw = new BackgroundWorker();


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
            mouse_event(MOUSEEVENTF_LEFTDOWN, System.Windows.Forms.Control.MousePosition.X, System.Windows.Forms.Control.MousePosition.Y, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, System.Windows.Forms.Control.MousePosition.X, System.Windows.Forms.Control.MousePosition.Y, 0, 0);
        }

        public static void LeftDown()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, System.Windows.Forms.Control.MousePosition.X, System.Windows.Forms.Control.MousePosition.Y, 0, 0);
        }

        public static void LeftUp()
        {
            mouse_event(MOUSEEVENTF_LEFTUP, System.Windows.Forms.Control.MousePosition.X, System.Windows.Forms.Control.MousePosition.Y, 0, 0);
        }

        public static void RightClick()
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, System.Windows.Forms.Control.MousePosition.X, System.Windows.Forms.Control.MousePosition.Y, 0, 0);
            mouse_event(MOUSEEVENTF_RIGHTUP, System.Windows.Forms.Control.MousePosition.X, System.Windows.Forms.Control.MousePosition.Y, 0, 0);
        }

        public static void RightDown()
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, System.Windows.Forms.Control.MousePosition.X, System.Windows.Forms.Control.MousePosition.Y, 0, 0);
        }

        public static void RightUp()
        {
            mouse_event(MOUSEEVENTF_RIGHTUP, System.Windows.Forms.Control.MousePosition.X, System.Windows.Forms.Control.MousePosition.Y, 0, 0);
        }


        public Form1()
        {

            bgw.DoWork += Bgw_DoWork;
            InitializeComponent();


        }

       


        private void button1_Click(object sender, EventArgs e)
        {
            while (true)
            {
                Thread.Sleep(120000);
                RightClick();
                Thread.Sleep(1000);
                MoveM(-10, -10);
                LeftClick();
                MoveM(10, 10);
            }
            //if (button1.Text == "Start")
            //{
            //    button1.Text = "Stop";
            //    bgw.RunWorkerAsync();
            //}
            //else
            //    button1.Invoke(new Action(() => { button1.Text = "Start"; }));
        }

        private void Bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            while (button1.Text == "Stop")
            {

                if(count++ % 12 == 0)
                {
                    //runQuery();
                }
                Thread.Sleep(300000);
                RightClick();
                Thread.Sleep(1000);
                MoveM(-10, -10);
                LeftClick();
                MoveM(10, 10);
            }

        }

        private void runQuery()
        {
            InputSimulator sim = new InputSimulator();

            sim.Mouse.Sleep(5000).MoveMouseTo(500, 2500)
                .Sleep(1000).LeftButtonClick()
                .Sleep(1000).MoveMouseTo(500, 7000)
                .Sleep(1000).MoveMouseTo(13500, 7000).MoveMouseTo(13500, 9500).Sleep(1000).LeftButtonClick();

            
            sim.Keyboard.Sleep(7000)//.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_N)
            .TextEntry("select getdate()")
            ;

            sim.Mouse.Sleep(1000).MoveMouseTo(25000, 29000)
                .Sleep(1000).RightButtonClick()
                .Sleep(1000).MoveMouseTo(26000, 40000)
                .Sleep(1000).LeftButtonClick();

            AppendTextBox(DateTime.Now.ToLocalTime() + " run query");
        }

        public void AppendTextBox(string value)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(AppendTextBox), new object[] { value });
                return;
            }
            textBox1.Text += value;
        }

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
                ClickOnPoint(window, new Point( 300, 300));

                

                p.WaitForExit();
            }
            catch { }//proc should fail.
            try
            {
                if (p.HasExited)
                {
                    //....
                }
            }
            catch (System.InvalidOperationException ex)
            {
                //cry and weep about it here.
            }
            

        }

        [DllImport("user32.dll")]
        static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        internal static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

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

        public static void ClickOnPoint(IntPtr wndHandle, Point clientPoint)
        {
            var oldPos = Cursor.Position;

            /// get screen coordinates
            ClientToScreen(wndHandle, ref clientPoint);

            /// set cursor on coords, and press mouse
            Cursor.Position = new Point(clientPoint.X, clientPoint.Y);

            var inputMouseDown = new INPUT();
            inputMouseDown.Type = 0; /// input type mouse
            inputMouseDown.Data.Mouse.Flags = 0x0002; /// left button down

            var inputMouseUp = new INPUT();
            inputMouseUp.Type = 0; /// input type mouse
            inputMouseUp.Data.Mouse.Flags = 0x0004; /// left button up

            var inputs = new INPUT[] { inputMouseDown, inputMouseUp };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));

            /// return mouse 
            //Cursor.Position = oldPos;
        }
    }

   
}
