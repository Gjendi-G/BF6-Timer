// Windows 10/11, .NET Framework 4.x. Keine AutoHotkey-Abhaengigkeit.
// Num0 (NumLock an/aus): 90 Sekunden starten oder neu starten.
// Lautlos, transparent, ohne Fokuswechsel; Beenden ueber das Tray-Symbol.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
namespace EinfacherTimer
{
    internal static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        private static void Main()
        {
            bool firstInstance;
            using (Mutex instance = new Mutex(true, @"Local\BF6_Timer_90s_Num0", out firstInstance))
            {
                if (!firstInstance) return;
                try
                {
                    SetProcessDPIAware();
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
                    using (TimerOverlay overlay = new TimerOverlay())
                        Application.Run(overlay);
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Message, "90s-Timer",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally { instance.ReleaseMutex(); }
            }
        }
    }

    internal static class OverlayLayout
    {
        public static Point WarningLocation(Rectangle screen, Size window)
        {
            return new Point(screen.Left + (screen.Width - window.Width) / 2,
                screen.Top + screen.Height * 3 / 10 - window.Height / 2);
        }
    }
    internal sealed class CountdownState
    {
        private long startedAt;
        public int RemainingSeconds { get; private set; }
        public bool Running { get; private set; }
        public bool WarningVisible
        { get { return Running && RemainingSeconds > 0 && RemainingSeconds <= 10; } }
        public CountdownState() { RemainingSeconds = 90; }
        public void Restart(long nowMilliseconds)
        {
            startedAt = nowMilliseconds;
            RemainingSeconds = 90;
            Running = true;
        }
        public void Update(long nowMilliseconds)
        {
            if (!Running) return;
            long elapsed = Math.Max(0L, nowMilliseconds - startedAt);
            RemainingSeconds = elapsed >= 90000L
                ? 0 : (int)((90000L - elapsed + 999L) / 1000L);
            if (RemainingSeconds == 0) Running = false;
        }
    }
    internal sealed class Num0KeyState
    {
        private readonly HashSet<IntPtr> pressed = new HashSet<IntPtr>();
        public bool IsNewPress(IntPtr device, ushort makeCode, ushort flags, ushort virtualKey)
        {
            if (makeCode != 0x52 || (flags & 6) != 0 || virtualKey == 255)
                return false;
            if ((flags & 1) != 0)
            {
                pressed.Remove(device);
                return false;
            }
            return pressed.Add(device);
        }
        public void RemoveDevice(IntPtr device) { pressed.Remove(device); }
    }

    internal class ClickThroughOverlay : Form
    {
        protected readonly Label message = new Label();
        private Font ownedFont;

        protected ClickThroughOverlay()
        {
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Black;
            TransparencyKey = Color.Black; // Schwarz wird vollstaendig transparent.
            TopMost = true;
            message.Dock = DockStyle.Fill;
            message.TextAlign = ContentAlignment.MiddleCenter;
            message.ForeColor = Color.Red;
            message.BackColor = Color.Transparent;
            Controls.Add(message);
        }

        protected void SetTextFont(float pixels)
        {
            Font previous = ownedFont;
            ownedFont = new Font("Segoe UI", pixels, FontStyle.Bold, GraphicsUnit.Pixel);
            message.Font = ownedFont;
            if (previous != null) previous.Dispose();
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // WS_EX_LAYERED | TRANSPARENT | NOACTIVATE | TOOLWINDOW
                cp.ExStyle |= 0x00080000 | 0x00000020 | 0x08000000 | 0x00000080;
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0084) { m.Result = new IntPtr(-1); return; } // HTTRANSPARENT
            if (m.Msg == 0x0021) { m.Result = new IntPtr(3); return; }  // MA_NOACTIVATE
            base.WndProc(ref m);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && ownedFont != null)
            {
                ownedFont.Dispose();
                ownedFont = null;
            }
            base.Dispose(disposing);
        }
    }

    internal sealed class WarningOverlay : ClickThroughOverlay
    {
        public WarningOverlay()
        {
            Text = "Heli-Warnung";
            message.Text = "HELI UP SOON.";
            // Opacity bleibt 1: kraeftig rote Schrift, transparenter Hintergrund.
        }

        public void PositionOn(Rectangle screen)
        {
            float pixels = Math.Max(26f, Math.Min(72f, 48f * screen.Height / 1080f));
            SetTextFont(pixels);
            Size textSize = TextRenderer.MeasureText(message.Text, message.Font);
            int availableWidth = Math.Max(1, screen.Width - 24);
            if (textSize.Width > availableWidth)
            {
                SetTextFont(pixels * availableWidth / textSize.Width);
                textSize = TextRenderer.MeasureText(message.Text, message.Font);
            }
            ClientSize = new Size(textSize.Width + 16, textSize.Height + 8);
            Location = OverlayLayout.WarningLocation(screen, Size);
        }
    }

    internal sealed class TimerOverlay : ClickThroughOverlay
    {
        private const uint RidevRemove = 0x00000001;
        private const uint RidevInputSink = 0x00000100;
        private const uint RidevDevNotify = 0x00002000;
        private const uint RidInput = 0x10000003;

        [StructLayout(LayoutKind.Sequential)]
        private struct RawInputDevice
        {
            public ushort UsagePage, Usage;
            public uint Flags;
            public IntPtr Target;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RawInputHeader
        {
            public uint Type, Size;
            public IntPtr Device, WParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RawKeyboard
        {
            public ushort MakeCode, Flags, Reserved, VirtualKey;
            public uint Message, ExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RawKeyboardInput
        {
            public RawInputHeader Header;
            public RawKeyboard Keyboard;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RegisterRawInputDevices(
            RawInputDevice[] devices, uint count, uint size);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetRawInputData(IntPtr input, uint command,
            IntPtr data, ref uint size, uint headerSize);

        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly CountdownState countdown = new CountdownState();
        private readonly Num0KeyState keys = new Num0KeyState();
        private readonly System.Windows.Forms.Timer refresh = new System.Windows.Forms.Timer();
        private readonly WarningOverlay warning = new WarningOverlay();
        private readonly NotifyIcon tray = new NotifyIcon();
        private readonly ContextMenuStrip menu = new ContextMenuStrip();
        private bool inputRegistered;
        private bool resourcesDisposed;
        private int shownSeconds = -1;

        public TimerOverlay()
        {
            Text = "90s-Timer";
            ClientSize = new Size(164, 70);
            SetTextFont(31f * 96f / 72f); // Bisherige 31 pt bei 96 dpi.
            Opacity = 0.90;
            PositionOverlays();

            menu.Items.Add("Beenden", null, delegate { Close(); });
            tray.Icon = SystemIcons.Application;
            tray.Text = "90s-Timer - Num0: Start / Neustart";
            tray.ContextMenuStrip = menu;
            tray.Visible = true;

            refresh.Interval = 80;
            refresh.Tick += delegate
            {
                countdown.Update(clock.ElapsedMilliseconds);
                UpdateDisplay();
                if (!countdown.Running) refresh.Stop();
            };
            UpdateDisplay(); // Anfangs 01:30, noch kein laufender Countdown.
        }

        private void PositionOverlays()
        {
            Rectangle screen = Screen.PrimaryScreen.Bounds;
            Location = new Point(screen.Left + 35, screen.Top + 45);
            warning.PositionOn(screen);
        }

        private void RestartCountdown()
        {
            warning.Hide();
            countdown.Restart(clock.ElapsedMilliseconds);
            UpdateDisplay();
            refresh.Start(); // Derselbe Timer; keine weiteren Tick-Handler.
        }

        private void UpdateDisplay()
        {
            int seconds = countdown.RemainingSeconds;
            if (seconds != shownSeconds)
            {
                shownSeconds = seconds;
                message.Text = string.Format("{0:00}:{1:00}", seconds / 60, seconds % 60);
            }
            if (countdown.WarningVisible)
            {
                if (!warning.Visible) warning.Show(this);
            }
            else if (warning.Visible) warning.Hide();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RawInputDevice keyboard = new RawInputDevice();
            keyboard.UsagePage = 1;
            keyboard.Usage = 6;
            // Nur mitlesen: keine Unterdrueckung der Tastatureingaben im Spiel.
            keyboard.Flags = RidevInputSink | RidevDevNotify;
            keyboard.Target = Handle;
            inputRegistered = RegisterRawInputDevices(new[] { keyboard }, 1,
                (uint)Marshal.SizeOf(typeof(RawInputDevice)));
            if (!inputRegistered)
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    "Num0 konnte nicht als Raw-Input-Taste eingerichtet werden.");
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (inputRegistered)
            {
                RawInputDevice keyboard = new RawInputDevice();
                keyboard.UsagePage = 1;
                keyboard.Usage = 6;
                keyboard.Flags = RidevRemove;
                keyboard.Target = IntPtr.Zero;
                RegisterRawInputDevices(new[] { keyboard }, 1,
                    (uint)Marshal.SizeOf(typeof(RawInputDevice)));
                inputRegistered = false;
            }
            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x00FF) // WM_INPUT
            {
                if (IsNum0Press(m.LParam)) RestartCountdown();
                // Auch WM_INPUT an DefWindowProc weitergeben (Windows-Cleanup).
            }
            else if (m.Msg == 0x00FE && m.WParam.ToInt64() == 2) // GIDC_REMOVAL
                keys.RemoveDevice(m.LParam);
            else if (m.Msg == 0x007E && !Disposing) // WM_DISPLAYCHANGE, UI-Thread
                BeginInvoke(new MethodInvoker(PositionOverlays));
            base.WndProc(ref m);
        }

        private bool IsNum0Press(IntPtr inputHandle)
        {
            uint size = 0;
            uint headerSize = (uint)Marshal.SizeOf(typeof(RawInputHeader));
            uint minimumSize = (uint)Marshal.SizeOf(typeof(RawKeyboardInput));
            if (GetRawInputData(inputHandle, RidInput, IntPtr.Zero, ref size, headerSize) != 0
                || size < minimumSize || size > int.MaxValue)
                return false;
            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                uint read = GetRawInputData(inputHandle, RidInput, buffer, ref size, headerSize);
                if (read == uint.MaxValue || read < minimumSize) return false;
                RawKeyboardInput input = (RawKeyboardInput)Marshal.PtrToStructure(
                    buffer, typeof(RawKeyboardInput));
                return input.Header.Type == 1
                    && keys.IsNewPress(input.Header.Device, input.Keyboard.MakeCode,
                        input.Keyboard.Flags, input.Keyboard.VirtualKey);
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !resourcesDisposed)
            {
                resourcesDisposed = true;
                refresh.Stop();
                refresh.Dispose();
                warning.Dispose();
                tray.Visible = false;
                tray.Dispose();
                menu.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
