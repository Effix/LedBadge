using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LedBadgeLib
{
    public static class ScreenCapture
    {
        public static void ReadScreenAtMousePosition(byte[] intermediateImage, int width, int height)
        {
            var pos = ScreenCapture.GetMousePosition();
            ReadScreen(intermediateImage, pos.X, pos.Y, width, height);
        }

        public static void ReadScreen(byte[] intermediateImage, int srcX, int srcY, int width, int height)
        {
            var pos = ScreenCapture.GetMousePosition();
            using(var b = ScreenCapture.Capture(
                (int)srcX - width / 2, 
                (int)srcY - height / 2, 
                width, height))
            {
                GDI.ReadBitmap(intermediateImage, b, 0, 0, width, height);
            }
        }

        static Bitmap Capture(int x, int y, int width, int height)
        {
            IntPtr desktopWindow = GetDesktopWindow();
            IntPtr desktopContext = GetWindowDC(desktopWindow);
            IntPtr captureContext = CreateCompatibleDC(desktopContext);
            IntPtr captureBitmap = CreateCompatibleBitmap(desktopContext, width, height);
            IntPtr oldBitmap = SelectObject(captureContext, captureBitmap);
            BitBlt(
                captureContext, 0, 0, width, height,
                desktopContext, x, y,
                CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);
            Bitmap bmp = Bitmap.FromHbitmap(captureBitmap);
            SelectObject(captureContext, oldBitmap);
            DeleteObject(captureBitmap);
            DeleteDC(captureContext);
            ReleaseDC(desktopWindow, desktopContext);
            return bmp;
        }

        static System.Drawing.Point GetMousePosition()
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new System.Drawing.Point(w32Mouse.X, w32Mouse.Y);
        }

        [DllImport("gdi32.dll")]
        static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int
        wDest, int hDest, IntPtr hdcSource, int xSrc, int ySrc, CopyPixelOperation rop);
        [DllImport("user32.dll")]
        static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDc);
        [DllImport("gdi32.dll")]
        static extern IntPtr DeleteDC(IntPtr hDc);
        [DllImport("gdi32.dll")]
        static extern IntPtr DeleteObject(IntPtr hDc);
        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")]
        static extern IntPtr SelectObject(IntPtr hdc, IntPtr bmp);
        [DllImport("user32.dll")]
        static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll")]
        static extern IntPtr GetWindowDC(IntPtr ptr);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };
    }
}
