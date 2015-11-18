using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LedBadgeLib
{
    public enum CommandCodes: byte
    {
        Nop,
        Ping,
        Version,
        Swap,
        PollInputs,
        SetBrightness,
        SetPix,
        GetPix,
        GetPixRect,
        SolidFill,
        Fill,
        Copy,
        SetPowerOnImage,
        SetHoldTimings
    }

    public enum Target: byte
    {
        BackBuffer,
        FrontBuffer
    }

    public static class BadgeCaps
    {
        public const int Width = 48;
        public const int Height = 12;
        public const int BitsPerPixel = 2;
        public const int ColorValues = 4;
        public const int PixelsPerByte = 8 / BitsPerPixel;
        public const int FrameStride = Width * BitsPerPixel / 8;
        public const int FrameSize = FrameStride * Height;
        public const int IntermediateFrameStride = Width;
        public const int IntermediateFrameSize = IntermediateFrameStride * Height;
    }

    public static class BadgeCommands
    {
        public static void Nop(Stream stream)
        {
            stream.WriteByte((byte)CommandCodes.Nop << 4);
        }

        public static void Ping(Stream stream, int cookie)
        {
            System.Diagnostics.Debug.Assert(cookie >= 0);
            System.Diagnostics.Debug.Assert(cookie < 16);

            stream.WriteByte((byte)(((byte)CommandCodes.Ping << 4) | cookie));
        }

        public static void GetVersion(Stream stream)
        {
            stream.WriteByte((byte)CommandCodes.Version << 4);
        }

        public static void Swap(Stream stream)
        {
            stream.WriteByte((byte)CommandCodes.Swap << 4);
        }

        public static void PollInputs(Stream stream)
        {
            stream.WriteByte((byte)CommandCodes.PollInputs << 4);
        }

        public static void SetBrightness(Stream stream, byte brightness)
        {
            stream.WriteByte((byte)CommandCodes.SetBrightness << 4);
            stream.WriteByte(brightness);
        }

        public static void SetPixel(Stream stream, int x, int y, Target target, int color)
        {
            System.Diagnostics.Debug.Assert(x >= 0);
            System.Diagnostics.Debug.Assert(x < BadgeCaps.Width);
            System.Diagnostics.Debug.Assert(y >= 0);
            System.Diagnostics.Debug.Assert(y < BadgeCaps.Height);
            System.Diagnostics.Debug.Assert(color >= 0 && color <= 3);

            stream.WriteByte((byte)((byte)CommandCodes.SetPix << 4));
            stream.WriteByte((byte)x);
            stream.WriteByte((byte)(((byte)y << 4) | ((byte)target << 2) | color));
        }

        public static void GetPixel(Stream stream, int x, int y, Target target)
        {
            System.Diagnostics.Debug.Assert(x >= 0);
            System.Diagnostics.Debug.Assert(x < BadgeCaps.Width);
            System.Diagnostics.Debug.Assert(y >= 0);
            System.Diagnostics.Debug.Assert(y < BadgeCaps.Height);

            stream.WriteByte((byte)((byte)CommandCodes.GetPix << 4));
            stream.WriteByte((byte)x);
            stream.WriteByte((byte)(((byte)y << 4) | ((byte)target << 2)));
        }

        public static void GetPixelRect(Stream stream, int x, int y, int width, int height, Target target)
        {
            System.Diagnostics.Debug.Assert(x >= 0);
            System.Diagnostics.Debug.Assert(x < BadgeCaps.Width);
            System.Diagnostics.Debug.Assert(width > 0);
            System.Diagnostics.Debug.Assert(x + width <= BadgeCaps.Width);
            System.Diagnostics.Debug.Assert(y >= 0);
            System.Diagnostics.Debug.Assert(y < BadgeCaps.Height);
            System.Diagnostics.Debug.Assert(height > 0);
            System.Diagnostics.Debug.Assert(y + height <= BadgeCaps.Height);

            stream.WriteByte((byte)(((byte)CommandCodes.GetPixRect << 4) | ((byte)target << 2)));
            stream.WriteByte((byte)x);
            stream.WriteByte((byte)width);
            stream.WriteByte((byte)((y << 4) | height));
        }

        public static void SolidFillRect(Stream stream, int x, int y, int width, int height, Target target, int color)
        {
            System.Diagnostics.Debug.Assert(x >= 0);
            System.Diagnostics.Debug.Assert(x < BadgeCaps.Width);
            System.Diagnostics.Debug.Assert(width > 0);
            System.Diagnostics.Debug.Assert(x + width <= BadgeCaps.Width);
            System.Diagnostics.Debug.Assert(y >= 0);
            System.Diagnostics.Debug.Assert(y < BadgeCaps.Height);
            System.Diagnostics.Debug.Assert(height > 0);
            System.Diagnostics.Debug.Assert(y + height <= BadgeCaps.Height);
            System.Diagnostics.Debug.Assert(color >= 0 && color <= 3);

            stream.WriteByte((byte)(((byte)CommandCodes.SolidFill << 4) | ((byte)target << 2) | color));
            stream.WriteByte((byte)x);
            stream.WriteByte((byte)width);
            stream.WriteByte((byte)((y << 4) | height));
        }

        public static void FillRect(Stream stream, int x, int y, int width, int height, Target target, byte[] data)
        {
            System.Diagnostics.Debug.Assert(x >= 0);
            System.Diagnostics.Debug.Assert(x < BadgeCaps.Width);
            System.Diagnostics.Debug.Assert(width > 0);
            System.Diagnostics.Debug.Assert(x + width <= BadgeCaps.Width);
            System.Diagnostics.Debug.Assert(y >= 0);
            System.Diagnostics.Debug.Assert(y < BadgeCaps.Height);
            System.Diagnostics.Debug.Assert(height > 0);
            System.Diagnostics.Debug.Assert(y + height <= BadgeCaps.Height);
            System.Diagnostics.Debug.Assert(data.Length == (width * height + 3) / 4);

            stream.WriteByte((byte)(((byte)CommandCodes.Fill << 4) | ((byte)target << 2)));
            stream.WriteByte((byte)x);
            stream.WriteByte((byte)width);
            stream.WriteByte((byte)((y << 4) | height));
            stream.Write(data, 0, data.Length);
        }

        public static void CopyRect(Stream stream, int srcX, int srcY, int width, int height, int dstX, int dstY, Target srcTarget, Target dstTarget)
        {
            System.Diagnostics.Debug.Assert(srcX >= 0);
            System.Diagnostics.Debug.Assert(srcX < BadgeCaps.Width);
            System.Diagnostics.Debug.Assert(dstX >= 0);
            System.Diagnostics.Debug.Assert(dstX < BadgeCaps.Width);
            System.Diagnostics.Debug.Assert(width > 0);
            System.Diagnostics.Debug.Assert(srcX + width <= BadgeCaps.Width);
            System.Diagnostics.Debug.Assert(dstX + width <= BadgeCaps.Width);
            System.Diagnostics.Debug.Assert(srcY >= 0);
            System.Diagnostics.Debug.Assert(srcY < BadgeCaps.Height);
            System.Diagnostics.Debug.Assert(dstY >= 0);
            System.Diagnostics.Debug.Assert(dstY < BadgeCaps.Height);
            System.Diagnostics.Debug.Assert(height > 0);
            System.Diagnostics.Debug.Assert(srcY + height <= BadgeCaps.Height);
            System.Diagnostics.Debug.Assert(dstY + height <= BadgeCaps.Height);

            stream.WriteByte((byte)((byte)CommandCodes.Copy << 4));
            stream.WriteByte((byte)srcX);
            stream.WriteByte((byte)dstX);
            stream.WriteByte((byte)((srcY << 4) | dstY));
            stream.WriteByte((byte)width);
            stream.WriteByte((byte)((height << 4) | ((byte)srcTarget << 2) | (byte)dstTarget));
        }

        public static void SetPowerOnImage(Stream stream)
        {
            stream.WriteByte((byte)CommandCodes.SetPowerOnImage << 4);
        }

        public static void SetHoldTimings(Stream stream, int a, int b, int c)
        {
            System.Diagnostics.Debug.Assert(a >= 0 && a <= 15);
            System.Diagnostics.Debug.Assert(b >= 0 && b <= 15);
            System.Diagnostics.Debug.Assert(c >= 0 && c <= 15);

            stream.WriteByte((byte)(((byte)CommandCodes.SetHoldTimings << 4) | a));
            stream.WriteByte((byte)((b << 4) | c));
        }
    }
}
