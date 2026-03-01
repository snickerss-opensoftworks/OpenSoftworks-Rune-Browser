using System;
using System.Runtime.InteropServices;

namespace RuneS.Helpers
{
    public static class WindowEffectsHelper
    {
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE  = 20;
        private const int DWMWA_BORDER_COLOR             = 34;

        private enum DWM_WINDOW_CORNER_PREFERENCE { DEFAULT = 0, DONOTROUND = 1, ROUND = 2, ROUNDSMALL = 3 }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int size);

        public static void ApplyRoundedCorners(IntPtr hwnd, bool small = false)
        {
            try
            {
                int pref = (int)(small ? DWM_WINDOW_CORNER_PREFERENCE.ROUNDSMALL
                                       : DWM_WINDOW_CORNER_PREFERENCE.ROUND);
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
            }
            catch { }
        }

        public static void ApplyDarkMode(IntPtr hwnd, bool dark)
        {
            try { int v = dark ? 1 : 0; DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref v, sizeof(int)); }
            catch { }
        }

        public static void ApplyBorderColor(IntPtr hwnd, System.Drawing.Color color)
        {
            try
            {
                int bgr = color.B << 16 | color.G << 8 | color.R;
                DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref bgr, sizeof(int));
            }
            catch { }
        }
    }
}
