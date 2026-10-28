using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace KeepSessionAlive
{
    /// <summary>
    /// Spawns gesture_tracker.py, reads MediaPipe gesture labels + hand landmarks from its stdout,
    /// and raises events the caller maps to mouse actions.
    /// </summary>
    internal sealed class GestureEngine : IDisposable
    {
        // ── Gesture vocabulary ────────────────────────────────────────────────
        public enum Gesture
        {
            None,
            Move,        // ✊ fist (trained)          → move cursor
            LeftClick,   // ☝ index only (trained)     → left click once
            RightClick,  // 👌 OK sign (geometry rule) → right click once
            Scroll,      // ✋ open palm (trained)      → joystick scroll
        }

        // ── Events ────────────────────────────────────────────────────────────
        /// Normalized cursor position [0-1, 0-1].
        public event Action<float, float> CursorMoved;
        public event Action LeftClicked;
        public event Action RightClicked;
        /// Wheel delta: positive = down, negative = up.
        public event Action<int> Scrolled;
        /// Horizontal wheel delta: positive = right, negative = left.
        public event Action<int> ScrolledHorizontal;
        /// Fired when the active gesture changes.
        public event Action<Gesture> GestureChanged;
        /// Diagnostic / error messages from the Python process (stderr lines).
        public event Action<string> DiagnosticMessage;

        // ── Tuning ────────────────────────────────────────────────────────────
        // Minimum model confidence to accept a trained label
        private const float MinScore = 0.6f;
        // A new pose must be held this long before it takes effect (filters flicker)
        private const int StableMs = 150;

        // Camera-frame box (normalized) mapped to the full screen; smaller box = more sensitive.
        // Bottom is kept high because the hand leaves the frame before reaching the edge.
        private const float ActiveLeft   = 0.20f;
        private const float ActiveRight  = 0.80f;
        private const float ActiveTop    = 0.15f;
        private const float ActiveBottom = 0.65f;
        private const float SmoothAlpha  = 0.3f;

        // OK sign: thumb–index tip gap in hand-size units (wrist → middle knuckle)
        private const float OkPinch = 0.30f;

        // Joystick scroll, offsets in hand-size units from where the palm first appeared
        private const float ScrollDeadZone = 0.40f;
        private const float ScrollRate     = 10f;  // notches per second per hand-size beyond the dead zone
        private const int   WheelDelta     = 120;

        // ── State ─────────────────────────────────────────────────────────────
        private Process _proc;
        private Thread  _reader;
        private Thread  _errReader;
        private volatile bool _stop;

        private Gesture  _current   = Gesture.None;
        private Gesture  _candidate = Gesture.None;
        private DateTime _candidateSince;

        private float _smoothX = 0.5f, _smoothY = 0.5f;

        // Recent cursor positions, used to undo drift when a click fires
        private struct CursorSample
        {
            public readonly DateTime Time;
            public readonly float X, Y;
            public CursorSample(DateTime time, float x, float y) { Time = time; X = x; Y = y; }
        }
        private readonly Queue<CursorSample> _cursorHistory = new Queue<CursorSample>();
        private const int HistoryMs = 1000;
        // How far before the pose change to rewind; raise if clicks still land off target
        private const int RewindMs  = 150;

        private float    _scrollCenterX, _scrollCenterY;
        private float    _scrollAccumX, _scrollAccumY;
        private DateTime _lastFrame;

        private static readonly Regex LabelRegex = new Regex("\"g\":\\s*\"([^\"]*)\"", RegexOptions.Compiled);
        private static readonly Regex ScoreRegex = new Regex("\"s\":\\s*([0-9.eE+-]+)", RegexOptions.Compiled);

        // ── Lifecycle ─────────────────────────────────────────────────────────
        public bool IsRunning => _proc != null && !_proc.HasExited;

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

        // ── Frame processing ──────────────────────────────────────────────────
        private void ProcessLine(string line)
        {
            DateTime now = DateTime.UtcNow;
            float dt = _lastFrame == default(DateTime) ? 0f : (float)(now - _lastFrame).TotalSeconds;
            _lastFrame = now;

            float[] pts = line.Contains("\"lm\": null") || line.Contains("\"lm\":null")
                ? null : ParseLandmarks(line);
            if (pts == null)
            {
                Classify(Gesture.None, now);
                return;
            }

            float Lx(int i) => pts[i * 2];
            float Ly(int i) => pts[i * 2 + 1];
            float D(int a, int b) => Dist(Lx(a), Ly(a), Lx(b), Ly(b));

            float handSize = D(0, 9);
            if (handSize < 0.01f) { Classify(Gesture.None, now); return; }

            // OK sign isn't a trained label: thumb+index ring with middle/ring/pinky extended.
            // Checked first because the model tends to call it Open_Palm.
            bool Extended(int tip, int pip) => D(tip, 0) > D(pip, 0) * 1.15f;
            bool okSign = D(4, 8) / handSize < OkPinch
                          && Extended(12, 10) && Extended(16, 14) && Extended(20, 18);

            Gesture seen;
            if (okSign)
                seen = Gesture.RightClick;
            else
                seen = FromLabel(ParseLabel(line, out float score), score);

            Classify(seen, now);

            // Palm centre (wrist + four knuckles) is stable in every pose, unlike fingertips
            float palmX = (Lx(0) + Lx(5) + Lx(9) + Lx(13) + Lx(17)) / 5f;
            float palmY = (Ly(0) + Ly(5) + Ly(9) + Ly(13) + Ly(17)) / 5f;

            // Only move while this frame is also a fist, so the cursor freezes the moment the hand
            // starts changing shape instead of after the new pose is confirmed
            if (_current == Gesture.Move && seen == Gesture.Move)
            {
                _smoothX = SmoothAlpha * palmX + (1f - SmoothAlpha) * _smoothX;
                _smoothY = SmoothAlpha * palmY + (1f - SmoothAlpha) * _smoothY;
                // Mirror X so movement feels natural (camera flips horizontally)
                float cx = MapToScreen(1f - _smoothX, ActiveLeft, ActiveRight);
                float cy = MapToScreen(_smoothY, ActiveTop, ActiveBottom);
                CursorMoved?.Invoke(cx, cy);

                _cursorHistory.Enqueue(new CursorSample(now, cx, cy));
                while (_cursorHistory.Count > 0 && (now - _cursorHistory.Peek().Time).TotalMilliseconds > HistoryMs)
                    _cursorHistory.Dequeue();
            }
            else if (_current != Gesture.Move)
            {
                // Track the hand anyway so the cursor doesn't jump on the next fist
                _smoothX = palmX;
                _smoothY = palmY;
            }

            if (_current == Gesture.Scroll)
                Joystick(palmX, palmY, handSize, dt);
        }

        private static Gesture FromLabel(string label, float score)
        {
            if (score < MinScore) return Gesture.None;
            switch (label)
            {
                case "Closed_Fist": return Gesture.Move;
                case "Pointing_Up": return Gesture.LeftClick;
                case "Open_Palm":   return Gesture.Scroll;
                default:            return Gesture.None;
            }
        }

        // A pose must persist for StableMs before it becomes current
        private void Classify(Gesture seen, DateTime now)
        {
            if (seen != _candidate)
            {
                _candidate      = seen;
                _candidateSince = now;
                return;
            }
            if (seen != _current && (now - _candidateSince).TotalMilliseconds >= StableMs)
                SetGesture(seen);
        }

        private void SetGesture(Gesture g)
        {
            if (g == _current) return;
            _current = g;

            if (g == Gesture.LeftClick || g == Gesture.RightClick)
            {
                RewindCursor();
                if (g == Gesture.LeftClick) LeftClicked?.Invoke();
                else                        RightClicked?.Invoke();
            }
            if (g == Gesture.Scroll)
            {
                // Centre is set on the first scroll frame
                _scrollCenterX = float.NaN;
                _scrollAccumX = _scrollAccumY = 0f;
            }

            GestureChanged?.Invoke(g);
        }

        // Put the cursor back where it was just before the hand began changing shape,
        // undoing drift from the last fist frames.
        private void RewindCursor()
        {
            DateTime target = _candidateSince.AddMilliseconds(-RewindMs);
            CursorSample? pick = null;
            foreach (CursorSample s in _cursorHistory)
            {
                if (s.Time > target) break;
                pick = s;
            }
            if (pick.HasValue)
                CursorMoved?.Invoke(pick.Value.X, pick.Value.Y);
            _cursorHistory.Clear();
        }

        // Palm offset from where it appeared sets scroll direction and speed; centre = stop.
        private void Joystick(float palmX, float palmY, float handSize, float dt)
        {
            if (float.IsNaN(_scrollCenterX))
            {
                _scrollCenterX = palmX;
                _scrollCenterY = palmY;
                return;
            }

            // Mirror X: moving the hand to your right scrolls right
            float offX = -(palmX - _scrollCenterX) / handSize;
            float offY =  (palmY - _scrollCenterY) / handSize;

            // Dominant axis only, so a slightly diagonal hand doesn't drift sideways
            if (Math.Abs(offY) >= Math.Abs(offX))
            {
                _scrollAccumX = 0f;
                _scrollAccumY += Rate(offY) * dt;
                Emit(ref _scrollAccumY, Scrolled);
            }
            else
            {
                _scrollAccumY = 0f;
                _scrollAccumX += Rate(offX) * dt;
                Emit(ref _scrollAccumX, ScrolledHorizontal);
            }
        }

        private static float Rate(float offset)
        {
            float beyond = Math.Abs(offset) - ScrollDeadZone;
            return beyond <= 0f ? 0f : Math.Sign(offset) * beyond * ScrollRate;
        }

        private static void Emit(ref float accum, Action<int> scroll)
        {
            while (accum >= 1f)  { scroll?.Invoke(WheelDelta);  accum -= 1f; }
            while (accum <= -1f) { scroll?.Invoke(-WheelDelta); accum += 1f; }
        }

        // ── JSON parsing ──────────────────────────────────────────────────────
        private static string ParseLabel(string json, out float score)
        {
            score = 0f;
            Match g = LabelRegex.Match(json);
            Match s = ScoreRegex.Match(json);
            if (s.Success)
                float.TryParse(s.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out score);
            return g.Success ? g.Groups[1].Value : "None";
        }

        // Parses the "lm":[[x,y],...21 pairs...] array without a JSON library dependency.
        private static float[] ParseLandmarks(string json)
        {
            try
            {
                int start = json.IndexOf("[[");
                int end   = json.LastIndexOf("]]");
                if (start < 0 || end < 0 || end <= start) return null;

                string inner = json.Substring(start + 1, end - start);

                var result = new float[42];
                int idx = 0;

                foreach (string tok in inner.Split(']'))
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
