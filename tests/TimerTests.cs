using System;
using System.Drawing;

namespace EinfacherTimer
{
    internal static class TimerTests
    {
        private static int passed;

        private static void Check(string name, Action test)
        {
            test();
            passed++;
            Console.WriteLine("PASS " + name);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!object.Equals(expected, actual))
                throw new Exception("Expected " + expected + ", got " + actual);
        }

        public static void Main()
        {
            Check("Idle shows 90 without warning", delegate
            {
                CountdownState c = new CountdownState();
                c.Update(1000000);
                Equal(90, c.RemainingSeconds);
                Equal(false, c.Running);
                Equal(false, c.WarningVisible);
            });
            Check("Restart begins a full 90 seconds", delegate
            {
                CountdownState c = new CountdownState();
                c.Restart(12345);
                Equal(90, c.RemainingSeconds);
                Equal(true, c.Running);
                c.Update(13344);
                Equal(90, c.RemainingSeconds);
                c.Update(13345);
                Equal(89, c.RemainingSeconds);
            });
            Check("Warn at 10 seconds, never at 15 or 11", delegate
            {
                CountdownState c = new CountdownState();
                c.Restart(10000);
                c.Update(85000);
                Equal(15, c.RemainingSeconds);
                Equal(false, c.WarningVisible);
                c.Update(89999);
                Equal(11, c.RemainingSeconds);
                Equal(false, c.WarningVisible);
                c.Update(90000);
                Equal(10, c.RemainingSeconds);
                Equal(true, c.WarningVisible);
                c.Update(99999);
                Equal(1, c.RemainingSeconds);
                Equal(true, c.WarningVisible);
            });
            Check("Expiry clears warning and stops, including delayed ticks", delegate
            {
                CountdownState c = new CountdownState();
                c.Restart(0);
                c.Update(80000);
                c.Update(90000);
                Equal(0, c.RemainingSeconds);
                Equal(false, c.WarningVisible);
                Equal(false, c.Running);
                c.Restart(100000);
                c.Update(999999);
                Equal(0, c.RemainingSeconds);
                Equal(false, c.WarningVisible);
                Equal(false, c.Running);
            });
            Check("Reset immediately hides warning and resets its deadline", delegate
            {
                CountdownState c = new CountdownState();
                c.Restart(0);
                c.Update(85000);
                Equal(true, c.WarningVisible);
                c.Restart(85000);
                Equal(90, c.RemainingSeconds);
                Equal(false, c.WarningVisible);
                c.Update(90000);
                Equal(85, c.RemainingSeconds);
                Equal(false, c.WarningVisible);
                c.Update(164999);
                Equal(false, c.WarningVisible);
                c.Update(165000);
                Equal(10, c.RemainingSeconds);
                Equal(true, c.WarningVisible);
            });
            Check("Countdown restarts after completion and follows elapsed time", delegate
            {
                CountdownState c = new CountdownState();
                c.Restart(0);
                c.Update(91000);
                c.Restart(100000);
                c.Update(134200);
                Equal(56, c.RemainingSeconds);
                Equal(true, c.Running);
            });
            Check("Physical Num0 works for NumLock on and off", delegate
            {
                Num0KeyState keys = new Num0KeyState();
                IntPtr keyboard = new IntPtr(1);
                Equal(true, keys.IsNewPress(keyboard, 0x52, 0, 0x60));
                Equal(false, keys.IsNewPress(keyboard, 0x52, 1, 0x60));
                Equal(true, keys.IsNewPress(keyboard, 0x52, 0, 0x2D));
            });
            Check("Held Num0 does not reset until released", delegate
            {
                Num0KeyState keys = new Num0KeyState();
                IntPtr keyboard = new IntPtr(1);
                Equal(true, keys.IsNewPress(keyboard, 0x52, 0, 0x60));
                for (int i = 0; i < 20; i++)
                    Equal(false, keys.IsNewPress(keyboard, 0x52, 0, 0x60));
                Equal(false, keys.IsNewPress(keyboard, 0x52, 1, 0x60));
                Equal(true, keys.IsNewPress(keyboard, 0x52, 0, 0x60));
            });
            Check("Dedicated Insert, ScrollLock, E1 and fake events do nothing", delegate
            {
                Num0KeyState keys = new Num0KeyState();
                IntPtr keyboard = new IntPtr(1);
                Equal(false, keys.IsNewPress(keyboard, 0x52, 2, 0x2D));
                Equal(false, keys.IsNewPress(keyboard, 0x46, 0, 0x91));
                Equal(false, keys.IsNewPress(keyboard, 0x52, 4, 0x60));
                Equal(false, keys.IsNewPress(keyboard, 0x52, 0, 255));
                Equal(true, keys.IsNewPress(keyboard, 0x52, 0, 0x60));
            });
            Check("Multiple keyboards and reconnects do not stick", delegate
            {
                Num0KeyState keys = new Num0KeyState();
                IntPtr first = new IntPtr(1), second = new IntPtr(2);
                Equal(true, keys.IsNewPress(first, 0x52, 0, 0x60));
                Equal(true, keys.IsNewPress(second, 0x52, 0, 0x60));
                Equal(false, keys.IsNewPress(first, 0x52, 1, 0x60));
                Equal(false, keys.IsNewPress(second, 0x52, 0, 0x60));
                keys.RemoveDevice(second);
                Equal(true, keys.IsNewPress(second, 0x52, 0, 0x60));
            });
            Check("Warning is centered on full screen at multiple resolutions", delegate
            {
                Equal(new Point(760, 392), OverlayLayout.WarningLocation(
                    new Rectangle(0, 0, 1920, 1080), new Size(400, 80)));
                Equal(new Point(440, 248), OverlayLayout.WarningLocation(
                    new Rectangle(0, 0, 1280, 720), new Size(400, 80)));
                Equal(new Point(1720, 824), OverlayLayout.WarningLocation(
                    new Rectangle(0, 0, 3840, 2160), new Size(400, 80)));
                Equal(new Point(-1160, 392), OverlayLayout.WarningLocation(
                    new Rectangle(-1920, 0, 1920, 1080), new Size(400, 80)));
            });
            Console.WriteLine(passed + " tests passed.");
        }
    }
}
