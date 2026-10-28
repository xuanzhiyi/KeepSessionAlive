using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace KeepSessionAlive
{
    /// <summary>
    /// Spawns gesture_tracker.py, reads MediaPipe hand landmarks from its stdout,
    /// classifies gestures, and raises events the caller maps to mouse actions.
    /// </summary>
    internal sealed class GestureEngine : IDisposable
    {
        // ── Gesture vocabulary ────────────────────────────────────────────────
        public enum Gesture
        {
            None,
            Point,      // index extended only  → move cursor
            Scroll,     // thumb+index+middle tips bunched → clutch scroll
            OpenPalm,   // all five extended     → pause actions
        }

        // ── Events ────────────────────────────────────────────────────────────
        /// Normalized cursor position [0-1, 0-1] derived from index fingertip.
        public event Action<float, float> CursorMoved;
        /// Mouse left-click (pinch: index tip meets thumb tip).
        public event Action Clicked;
        /// Scroll delta: positive = down, negative = up.
        public event Action<float> Scrolled;
        /// Fired when the active gesture changes.
        public event Action<Gesture> GestureChanged;
        /// Diagnostic / error messages from the Python process (stderr lines).
        public event Action<string> DiagnosticMessage;

        // ── Win32 ─────────────────────────────────────────────────────────────
        [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll")] static extern void mouse_event(uint flags, int dx, int dy, int data, int extra);
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP   = 0x0004;
        private const uint MOUSEEVENTF_WHEEL    = 0x0800;

        // ── State ─────────────────────────────────────────────────────────────
        private Process _proc;
        private Thread  _reader;
        private Thread  _errReader;
        private volatile bool _stop;

        private float _smoothX = 0.5f, _smoothY = 0.5f;
        private const float SmoothAlpha = 0.25f;

        private Gesture  _current   = Gesture.None;
        private bool     _prevPinch;

        // Thresholds are in hand-size units (wrist → middle knuckle)
        private const float ClutchEngage   = 0.35f;
        private const float ClutchRelease  = 0.55f;
        private const float PinchThreshold = 0.30f;

        // Hand travel (hand-size units) per wheel notch; lower = faster scroll
        private const float ScrollStep  = 0.15f;

        // Camera-frame box (normalized) mapped to the full screen; smaller box = more sensitive.
        // Bottom is kept high because the hand leaves the frame before the fingertip reaches the edge.
        private const float ActiveLeft   = 0.20f;
        private const float ActiveRight  = 0.80f;
        private const float ActiveTop    = 0.15f;
        private const float ActiveBottom = 0.65f;
        private const float ScrollAlpha = 0.5f;
        private const int   WheelDelta  = 120;

        private bool  _scrolling;
        private float _scrollY;
        private float _scrollAccum;
        private DateTime _lastClick = DateTime.MinValue;
        private const int ClickCooldownMs = 600;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        public bool IsRunning => _proc != null && !_proc.HasExited;

        // Try these in order until one produces output on the first line.
        private static readonly string[] PythonCandidates = { "python", "py", "python3" };

        public void Start(string pythonExe = null)
        {
            if (IsRunning) return;

            string script = Path.Combine(
                Path.GetDirectoryName(Application.ExecutablePath),
                "gesture_tracker.py");

            if (!File.Exists(script))
                throw new FileNotFoundException(
                    "gesture_tracker.py not found next to the executable.", script);

            string exe = pythonExe ?? FindPython()
                ?? throw new InvalidOperationException(
                    "No Python interpreter found. Install Python and ensure 'python', 'py', or 'python3' is on PATH.");

            _stop = false;

            _proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName               = exe,
                    Arguments              = $"\"{script}\"",
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                }
            };
            _proc.Start();

            _reader    = new Thread(ReadLoop)    { IsBackground = true, Name = "GestureReader" };
            _errReader = new Thread(ReadErrLoop) { IsBackground = true, Name = "GestureErrReader" };
            _reader.Start();
            _errReader.Start();
        }

        public void Stop()
        {
            _stop = true;
            try { _proc?.Kill(); } catch { }
            _proc = null;
            SetGesture(Gesture.None);
        }

        public void Dispose() => Stop();

        // Probe each candidate by running "python --version"; return the first that exits cleanly.
        private static string FindPython()
        {
            foreach (string candidate in PythonCandidates)
            {
                try
                {
                    var p = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName               = candidate,
                            Arguments              = "--version",
                            UseShellExecute        = false,
                            CreateNoWindow         = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError  = true,
                        }
                    };
                    p.Start();
                    p.WaitForExit(1500);
                    if (p.ExitCode == 0) return candidate;
                }
                catch { }
            }
            return null;
        }

        // ── Background readers ────────────────────────────────────────────────
        private void ReadLoop()
        {
            try
            {
                string line;
                while (!_stop && (line = _proc?.StandardOutput?.ReadLine()) != null)
                    ProcessLine(line);
            }
            catch { }
        }

        private void ReadErrLoop()
        {
            try
            {
                string line;
                while (!_stop && (line = _proc?.StandardError?.ReadLine()) != null)
                    DiagnosticMessage?.Invoke(line);
            }
            catch { }
        }

        // ── Landmark processing ───────────────────────────────────────────────
        private void ProcessLine(string line)
        {
            if (line.Contains("\"lm\":null") || line.Contains("\"lm\": null"))
            {
                SetGesture(Gesture.None);
                return;
            }

            float[] pts = ParseLandmarks(line);
            if (pts == null || pts.Length < 42)
            {
                SetGesture(Gesture.None);
                return;
            }

            // Accessors for landmark i  (image coords: y=0 top, y=1 bottom)
            float Lx(int i) => pts[i * 2];
            float Ly(int i) => pts[i * 2 + 1];

            float D(int a, int b) => Dist(Lx(a), Ly(a), Lx(b), Ly(b));

            // Wrist → middle-knuckle length; makes thresholds independent of distance to camera
            float handSize = D(0, 9);
            if (handSize < 0.01f) { SetGesture(Gesture.None); return; }

            // Extended = tip clearly farther from wrist than its PIP joint (works at any hand angle)
            bool Extended(int tip, int pip) => D(tip, 0) > D(pip, 0) * 1.15f;
            bool indexUp  = Extended(8, 6);
            bool middleUp = Extended(12, 10);
            bool ringUp   = Extended(16, 14);
            bool pinkyUp  = Extended(20, 18);
            bool thumbOut = D(4, 17) > D(3, 17) * 1.1f;

            float thumbIndex  = D(4, 8)  / handSize;
            float thumbMiddle = D(4, 12) / handSize;
            float indexMiddle = D(8, 12) / handSize;

            // Clutch: thumb, index, middle tips bunched together; hysteresis stops flicker
            float spread = Math.Max(thumbIndex, Math.Max(thumbMiddle, indexMiddle));
            bool scrolling = _scrolling ? spread < ClutchRelease : spread < ClutchEngage;

            // Pinch click: thumb meets index while middle stays clear (otherwise it's the clutch)
            bool pinching = !scrolling && thumbIndex < PinchThreshold && thumbMiddle > ClutchRelease;

            // ── Classify gesture ─────────────────────────────────────────────
            Gesture g;
            if (scrolling)
                g = Gesture.Scroll;
            else if (indexUp && middleUp && ringUp && pinkyUp && thumbOut)
                g = Gesture.OpenPalm;
            else if (indexUp && !middleUp && !ringUp && !pinkyUp)
                g = Gesture.Point;
            else
                g = Gesture.None;

            SetGesture(g);

            // ── Smooth index fingertip position ──────────────────────────────
            _smoothX = SmoothAlpha * Lx(8) + (1f - SmoothAlpha) * _smoothX;
            _smoothY = SmoothAlpha * Ly(8) + (1f - SmoothAlpha) * _smoothY;

            if (g == Gesture.Point)
            {
                // Mirror X so movement feels natural (camera flips horizontally)
                CursorMoved?.Invoke(
                    MapToScreen(1f - _smoothX, ActiveLeft, ActiveRight),
                    MapToScreen(_smoothY, ActiveTop, ActiveBottom));
            }

            // ── Clutch scroll ────────────────────────────────────────────────
            float clutchY = (Ly(4) + Ly(8) + Ly(12)) / 3f;
            if (scrolling && !_scrolling)
            {
                // Just engaged: anchor here so the grab itself never scrolls
                _scrollY      = clutchY;
                _scrollAccum  = 0f;
            }
            else if (scrolling)
            {
                float prev = _scrollY;
                _scrollY = ScrollAlpha * clutchY + (1f - ScrollAlpha) * _scrollY;
                _scrollAccum += (_scrollY - prev) / handSize;

                while (_scrollAccum >= ScrollStep)  { Scrolled?.Invoke(WheelDelta);  _scrollAccum -= ScrollStep; }
                while (_scrollAccum <= -ScrollStep) { Scrolled?.Invoke(-WheelDelta); _scrollAccum += ScrollStep; }
            }
            _scrolling = scrolling;

            // Pinch click with debounce
            if (pinching && !_prevPinch &&
                (DateTime.UtcNow - _lastClick).TotalMilliseconds > ClickCooldownMs)
            {
                _lastClick = DateTime.UtcNow;
                Clicked?.Invoke();
            }
            _prevPinch = pinching;
        }

        private void SetGesture(Gesture g)
        {
            if (g == _current) return;
            _current = g;
            GestureChanged?.Invoke(g);
        }

        // ── JSON landmark parser ──────────────────────────────────────────────
        // Parses: {"lm":[[x,y],...21 pairs...]}  without a JSON library dependency.
        private static float[] ParseLandmarks(string json)
        {
            try
            {
                int start = json.IndexOf("[[");
                int end   = json.LastIndexOf("]]");
                if (start < 0 || end < 0 || end <= start) return null;

                // Slice out the array-of-arrays content
                string inner = json.Substring(start + 1, end - start);

                var result = new float[42];
                int idx = 0;

                // Each point is [x,y]; split on ']' to isolate each
                string[] tokens = inner.Split(']');
                foreach (string tok in tokens)
                {
                    int bracket = tok.IndexOf('[');
                    if (bracket < 0) continue;
                    string[] nums = tok.Substring(bracket + 1).Split(',');
                    if (nums.Length < 2) continue;

                    if (float.TryParse(nums[0].Trim(), NumberStyles.Float,
                            CultureInfo.InvariantCulture, out float x) &&
                        float.TryParse(nums[1].Trim(), NumberStyles.Float,
                            CultureInfo.InvariantCulture, out float y) &&
                        idx + 1 < result.Length)
                    {
                        result[idx++] = x;
                        result[idx++] = y;
                    }
                }

                return idx == 42 ? result : null;
            }
            catch { return null; }
        }

        private static float MapToScreen(float v, float min, float max)
        {
            float t = (v - min) / (max - min);
            return t < 0f ? 0f : t > 1f ? 1f : t;
        }

        private static float Dist(float x1, float y1, float x2, float y2)
        {
            float dx = x1 - x2, dy = y1 - y2;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
