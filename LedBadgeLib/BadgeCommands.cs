using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LedBadgeLib
{
    /// <summary>
    /// Raw command code values that begin the command packets for the badge.
    /// </summary>
    public enum CommandCodes: byte
    {
        /// <summary>No action.</summary>
        Nop,
        /// <summary>Asks the badge to return the given cookie.</summary>
        Ping,
        /// <summary>Requests the version of the running firmware.</summary>
        Version,
        /// <summary>Swaps the front/back render targets.</summary>
        Swap,
        /// <summary>Requests the button state.</summary>
        PollInputs,
        /// <summary>Adjusts the overall output brightness of the leds.</summary>
        SetBrightness,
        /// <summary>Writes a single pixel value to a buffer.</summary>
        SetPix,
        /// <summary>Requests a single pixel value from a buffer.</summary>
        GetPix,
        /// <summary>Requests a block of pixels from a buffer.</summary>
        GetPixRect,
        /// <summary>Writes a single value to a block of pixels in a buffer.</summary>
        SolidFill,
        /// <summary>Sets a block of pixels in a buffer to the given data (2bpp packed).</summary>
        Fill,
        /// <summary>Copies a block of pixels from a location in a buffer to another.</summary>
        Copy,
        /// <summary>Sets the initial frame when first powered up (saves the front buffer to non-volatile memory).</summary>
        SetPowerOnImage,
        /// <summary>Controls the gray scale levels by setting the hold levels between the bit-planes.</summary>
        SetHoldTimings,
        /// <summary>Sets the idle timeout duration and behavior.</summary>
        SetIdleTimeout,
        /// <summary>Queries the state of the input buffer.</summary>
        GetBufferFullness
    }

    /// <summary>
    /// Identifiers for the command read and write locations.
    /// </summary>
    public enum Target: byte
    {
        /// <summary>The buffer not being displayed. This can be modified without seeing flicker.</summary>
        BackBuffer,
        /// <summary>The buffer being scanned out to the display.</summary>
        FrontBuffer
    }

    /// <summary>
    /// Constant metrics describing different aspects of the badge.
    /// </summary>
    public static class BadgeCaps
    {
        /// <summary>Pixels across.</summary>
        public const int Width = 48;
        /// <summary>Pixels tall.</summary>
        public const int Height = 12;
        /// <summary>Number of bits per pixel in a packed image buffer.</summary>
        public const int BitsPerPixel = 2;
        /// <summary>Number of colors per pixel in a packed image buffer.</summary>
        public const int ColorValues = 4;
        /// <summary>Number of pixels per byte in a packed image buffer.</summary>
        public const int PixelsPerByte = 8 / BitsPerPixel;
        /// <summary>Number of bytes for an entire row of pixels for a full screen packed image buffer.</summary>
        public const int FrameStride = Width * BitsPerPixel / 8;
        /// <summary>Number of bytes for a full screen packed image buffer.</summary>
        public const int FrameSize = FrameStride * Height;
        /// <summary>Number of bytes for an entire row of pixels for a full screen unpacked image buffer (i.e., one byte per pixel).</summary>
        public const int IntermediateFrameStride = Width;
        /// <summary>Number of bytes for a full screen unpacked image buffer (i.e., one byte per pixel).</summary>
        public const int IntermediateFrameSize = IntermediateFrameStride * Height;
    }

    /// <summary>
    /// Methods to construct badge commands into a stream of data.
    /// </summary>
    public static class BadgeCommands
    {
        /// <summary>
        /// No action.
        /// </summary>
        public static void Nop(Stream stream)
        {
            stream.WriteByte((byte)CommandCodes.Nop << 4);
        }

        /// <summary>
        /// Asks the badge to return the given cookie.
        /// <param name="cookie">A 4-bit value ranging from 0 to 15.</param>
        /// </summary>
        public static void Ping(Stream stream, int cookie)
        {
            System.Diagnostics.Debug.Assert(cookie >= 0);
            System.Diagnostics.Debug.Assert(cookie < 16);

            stream.WriteByte((byte)(((byte)CommandCodes.Ping << 4) | cookie));
        }

        /// <summary>
        /// Requests the version of the running firmware.
        /// </summary>
        public static void GetVersion(Stream stream)
        {
            stream.WriteByte((byte)CommandCodes.Version << 4);
        }

        /// <summary>
        /// Swaps the front/back render targets.
        /// </summary>
        public static void Swap(Stream stream)
        {
            stream.WriteByte((byte)CommandCodes.Swap << 4);
        }

        /// <summary>
        /// Requests the button state.
        /// </summary>
        public static void PollInputs(Stream stream)
        {
            stream.WriteByte((byte)CommandCodes.PollInputs << 4);
        }

        /// <summary>
        /// Adjusts the overall output brightness of the leds.
        /// <param name="brightness">A value ranging from 0 to 255.</param>
        /// </summary>
        public static void SetBrightness(Stream stream, byte brightness)
        {
            stream.WriteByte((byte)CommandCodes.SetBrightness << 4);
            stream.WriteByte(brightness);
        }

        /// <summary>
        /// Writes a single pixel value to a buffer.
        /// <param name="x">The horizontal location ranging from 0 to 41.</param>
        /// <param name="y">The vertical location ranging from 0 to 11.</param>
        /// <param name="target">The buffer to modify.</param>
        /// <param name="color">The value (0-3) to send.</param>
        /// </summary>
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

        /// <summary>
        /// Requests a single pixel value from a buffer.
        /// <param name="x">The horizontal location ranging from 0 to 41.</param>
        /// <param name="y">The vertical location ranging from 0 to 11.</param>
        /// <param name="target">The buffer to query.</param>
        /// </summary>
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

        /// <summary>
        /// Requests a block of pixels from a buffer.
        /// <param name="x">The horizontal location ranging from 0 to 41.</param>
        /// <param name="y">The vertical location ranging from 0 to 11.</param>
        /// <param name="width">The width of the rectangle. The right edge (x + width) must not exceed the width of the badge.</param>
        /// <param name="height">The height of the rectangle. The bottom edge (y + height) must not exceed the height of the badge.</param>
        /// <param name="target">The buffer to query.</param>
        /// </summary>
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

        /// <summary>
        /// Writes a single value to a block of pixels in a buffer.
        /// <param name="x">The horizontal location ranging from 0 to 41.</param>
        /// <param name="y">The vertical location ranging from 0 to 11.</param>
        /// <param name="width">The width of the rectangle. The right edge (x + width) must not exceed the width of the badge.</param>
        /// <param name="height">The height of the rectangle. The bottom edge (y + height) must not exceed the height of the badge.</param>
        /// <param name="target">The buffer to query.</param>
        /// <param name="color">The value (0-3) to send.</param>
        /// </summary>
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

        /// <summary>
        /// Sets a block of pixels in a buffer to the given data (2bpp packed).
        /// <param name="x">The horizontal location ranging from 0 to 41.</param>
        /// <param name="y">The vertical location ranging from 0 to 11.</param>
        /// <param name="width">The width of the rectangle. The right edge (x + width) must not exceed the width of the badge.</param>
        /// <param name="height">The height of the rectangle. The bottom edge (y + height) must not exceed the height of the badge.</param>
        /// <param name="target">The buffer to query.</param>
        /// <param name="data">The pixels to send, packed tightly. The length of this buffer must match the pixel count, packed as 2bpp and rounded up to the nearest byte.</param>
        /// </summary>
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

        /// <summary>
        /// Copies a block of pixels from a location in a buffer to another.
        /// <param name="srcX">The horizontal location of the source rectangle ranging from 0 to 41.</param>
        /// <param name="srcY">The vertical location of the source rectangle ranging from 0 to 11.</param>
        /// <param name="width">The width of the rectangle. The right edge (x + width) must not exceed the width of the badge.</param>
        /// <param name="height">The height of the rectangle. The bottom edge (y + height) must not exceed the height of the badge.</param>
        /// <param name="dstX">The horizontal location of the destination rectangle ranging from 0 to 41.</param>
        /// <param name="dstY">The vertical location of the destination rectangle ranging from 0 to 11.</param>
        /// <param name="srcTarget">The buffer to read.</param>
        /// <param name="dstTarget">The buffer to write.</param>
        /// </summary>
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

        /// <summary>
        /// Sets the initial frame when first powered up (saves the front buffer to non-volatile memory).
        /// </summary>
        public static void SetPowerOnImage(Stream stream)
        {
            stream.WriteByte((byte)CommandCodes.SetPowerOnImage << 4);
        }

        /// <summary>
        /// Controls the gray scale levels by setting the hold levels between the bit-planes.
        /// The values are cumulative, so specifying 1, 3, 4 will hold for 1, 4, 8 refresh periods.
        /// <param name="a">Hold for darker gray values.</param>
        /// <param name="b">Hold for lighter gray values.</param>
        /// <param name="c">Hold for brightest values.</param>
        /// </summary>
        public static void SetHoldTimings(Stream stream, int a, int b, int c)
        {
            System.Diagnostics.Debug.Assert(a >= 1 && a <= 15);
            System.Diagnostics.Debug.Assert(b >= 1 && b <= 15);
            System.Diagnostics.Debug.Assert(c >= 1 && c <= 15);

            stream.WriteByte((byte)(((byte)CommandCodes.SetHoldTimings << 4) | a));
            stream.WriteByte((byte)((b << 4) | c));
        }

        /// <summary>
        /// Sets the idle timeout duration and behavior.
        /// <param name="fade">True to fade out and then fade back in. False to instantly change.</param>
        /// <param name="resetToBootImage">True to restore the power on image. False to clear to black.</param>
        /// <param name="timeout">Number of frames to wait before resetting. A value of 255 will disable the idle behavior.</param>
        /// </summary>
        public static void SetIdleTimeout(Stream stream, bool fade, bool resetToBootImage, int timeout)
        {
            System.Diagnostics.Debug.Assert(timeout >= 0 && timeout <= 255);
            
            stream.WriteByte((byte)(((byte)CommandCodes.SetIdleTimeout << 4) | ((fade ? 1 : 0) << 3) | ((resetToBootImage ? 1 : 0) << 2)));
            stream.WriteByte((byte)timeout);
        }

        /// <summary>
        /// Queries the state of the input buffer.
        /// </summary>
        public static void GetBufferFullness(Stream stream)
        {
            stream.WriteByte((byte)CommandCodes.GetBufferFullness << 4);
        }
    }
}
