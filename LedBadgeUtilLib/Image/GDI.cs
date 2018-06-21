using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LedBadgeLib
{
    public static class GDI
    {
        public static void ReadBitmap(byte[] intermediateImage, Bitmap bitmap, int srcX, int srcY, int stride, int width, int height)
        {
            var data = bitmap.LockBits(
                new System.Drawing.Rectangle(srcX, srcY, width, height), 
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            try
            {
                IntPtr p0 = data.Scan0;
                for(int y = 0, i0 = 0; y < height; ++y, p0 += data.Stride, i0 += stride)
                {
                    IntPtr p = p0;
                    for(int x = 0, i = i0; x < width; ++x, ++i, p += 4)
                    {
                        int val = Marshal.ReadInt32(p);
                        intermediateImage[i] = BadgeImage.ToGray((val >> 16) & 0xFF, (val >> 8) & 0xFF, (val >> 0) & 0xFF);
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }
    }
}
