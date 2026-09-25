using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Striverum
{
    public static class DarkModeHelper
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        // Windows 10 1809 - 1909
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        // Windows 10 20H1+ and Windows 11
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        // Windows 11 22000+
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;

        public static void ApplyDarkMode(Window window)
        {
            if (window == null) return;

            void Apply(IntPtr hwnd)
            {
                if (hwnd == IntPtr.Zero) return;
                try
                {
                    int trueVal = 1;
                    // Enable dark mode for window frame
                    if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref trueVal, sizeof(int)) != 0)
                    {
                        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref trueVal, sizeof(int));
                    }

                    // On Windows 11, set caption color to dark gray #181818 (COLORREF format: 0x00BBGGRR)
                    int captionColor = 0x00181818;
                    DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));

                    // Set caption title text color to #F2F2F2
                    int textColor = 0x00F2F2F2;
                    DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref textColor, sizeof(int));
                }
                catch { }
            }

            var helper = new WindowInteropHelper(window);
            if (helper.Handle != IntPtr.Zero)
            {
                Apply(helper.Handle);
            }
            else
            {
                window.SourceInitialized += (s, e) =>
                {
                    var hwnd = new WindowInteropHelper(window).Handle;
                    Apply(hwnd);
                };
            }
        }

        public static void EnableDarkModeForAllWindows()
        {
            EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler((s, e) =>
            {
                if (s is Window w)
                {
                    ApplyDarkMode(w);
                }
            }));
        }
    }
}
