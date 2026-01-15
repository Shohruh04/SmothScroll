using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace SmoothScroll;

public sealed class ScrollInterpolator : IDisposable
{
    private const int MOUSEEVENTF_WHEEL = 0x0800;

    private readonly object _lock = new();
    private readonly Thread _animationThread;
    private readonly Stopwatch _stopwatch;
    private readonly ManualResetEventSlim _wakeSignal;

    private double _velocity;
    private double _fractionalAccumulator;
    private int _targetX;
    private int _targetY;
    private bool _disposed;
    private bool _running = true;

    // Configuration - tune these for desired feel
    public double Friction { get; set; } = 0.85;          // Velocity multiplier per frame (lower = more friction)
    public double VelocityMultiplier { get; set; } = 2.5; // How much each scroll adds to velocity
    public double MinVelocity { get; set; } = 0.5;        // Stop threshold
    public int FrameIntervalMs { get; set; } = 8;         // ~120fps for smooth animation
    public double MaxVelocity { get; set; } = 2000;       // Cap velocity to prevent runaway

    public ScrollInterpolator()
    {
        _stopwatch = Stopwatch.StartNew();
        _wakeSignal = new ManualResetEventSlim(false);

        _animationThread = new Thread(AnimationLoop)
        {
            IsBackground = true,
            Priority = ThreadPriority.AboveNormal,
            Name = "SmoothScroll Animation"
        };
        _animationThread.Start();
    }

    public void AddScroll(int delta, int x, int y)
    {
        lock (_lock)
        {
            // Add to velocity with multiplier
            _velocity += delta * VelocityMultiplier;

            // Clamp velocity
            _velocity = Math.Clamp(_velocity, -MaxVelocity, MaxVelocity);

            _targetX = x;
            _targetY = y;

            // Wake up animation thread
            _wakeSignal.Set();
        }
    }

    private void AnimationLoop()
    {
        double lastTime = _stopwatch.Elapsed.TotalSeconds;

        while (_running)
        {
            double currentTime = _stopwatch.Elapsed.TotalSeconds;
            double deltaTime = currentTime - lastTime;
            lastTime = currentTime;

            double velocity;
            int x, y;

            lock (_lock)
            {
                if (Math.Abs(_velocity) < MinVelocity)
                {
                    _velocity = 0;
                    _fractionalAccumulator = 0;
                    _wakeSignal.Reset();
                }

                velocity = _velocity;
                x = _targetX;
                y = _targetY;

                if (Math.Abs(_velocity) >= MinVelocity)
                {
                    // Apply friction (exponential decay)
                    _velocity *= Math.Pow(Friction, deltaTime * 60); // Normalize to 60fps base
                }
            }

            if (Math.Abs(velocity) >= MinVelocity)
            {
                // Calculate scroll amount for this frame
                double scrollAmount = velocity * deltaTime * 60; // Normalize to 60fps

                // Accumulate fractional scroll
                _fractionalAccumulator += scrollAmount;

                // Extract integer portion to send
                int integerScroll = (int)_fractionalAccumulator;

                if (integerScroll != 0)
                {
                    _fractionalAccumulator -= integerScroll;
                    SendMouseWheel(integerScroll, x, y);
                }

                // High precision sleep
                PreciseSleep(FrameIntervalMs);
            }
            else
            {
                // Wait for next scroll event
                _wakeSignal.Wait(100);
            }
        }
    }

    private static void PreciseSleep(int milliseconds)
    {
        // Use SpinWait for more precise timing than Thread.Sleep
        var sw = Stopwatch.StartNew();
        var targetTicks = milliseconds * Stopwatch.Frequency / 1000;

        // Sleep for most of the time
        if (milliseconds > 1)
        {
            Thread.Sleep(milliseconds - 1);
        }

        // Spin for the remainder for precision
        while (sw.ElapsedTicks < targetTicks)
        {
            Thread.SpinWait(10);
        }
    }

    private static void SendMouseWheel(int delta, int x, int y)
    {
        var inputs = new INPUT[1];
        inputs[0].type = INPUT_MOUSE;
        inputs[0].union.mi.dwFlags = MOUSEEVENTF_WHEEL;
        inputs[0].union.mi.mouseData = delta;
        inputs[0].union.mi.time = 0;
        inputs[0].union.mi.dwExtraInfo = MouseHook.SMOOTH_SCROLL_SIGNATURE; // Mark as our synthetic event

        SendInput(1, inputs, Marshal.SizeOf<INPUT>());
    }

    public void Stop()
    {
        lock (_lock)
        {
            _velocity = 0;
            _fractionalAccumulator = 0;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _running = false;
        _wakeSignal.Set();
        _animationThread.Join(500);
        _wakeSignal.Dispose();

        _disposed = true;
    }

    #region P/Invoke

    private const int INPUT_MOUSE = 0;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public INPUTUNION union;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public int mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern IntPtr GetMessageExtraInfo();

    #endregion
}
