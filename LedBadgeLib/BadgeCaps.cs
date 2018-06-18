using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LedBadgeLib
{
    /// <summary>
    /// Constant metrics describing different aspects of the badge.
    /// </summary>
    public class BadgeCaps
    {
        public BadgeCaps(int version, int width, int height, int bitDepth, SupportedFeatures caps, int baud)
        {
            Version = version;
            Width = width;
            Height = height;
            BitsPerPixel = bitDepth;
            ColorValues = 1 << BitsPerPixel;
            WidthInBlocks = (width + 7) / 8;
            BytesPerBlock = bitDepth;
            FrameStride = WidthInBlocks * BytesPerBlock;
            FrameSize = FrameStride * Height;
            IntermediateFrameStride = Width;
            IntermediateFrameSize = IntermediateFrameStride * Height;
            SupportedFeatures = caps;
            Baud = baud;
        }

        /// <summary>Number of pixels packed into one block.</summary>
        public static int PixelsPerBlockBitPlane = 8;
        /// <summary>Number of bits per pixel in an intermediate image buffer.</summary>
        public static int IntermediateBitsPerPixel = 8;

        /// <summary>Version of the device firmware.</summary>
        public int Version { get; private set; }
        /// <summary>Pixels across.</summary>
        public int Width { get; private set; }
        /// <summary>Pixels tall.</summary>
        public int Height { get; private set; }
        /// <summary>Number of bits per pixel in a packed image buffer.</summary>
        public int BitsPerPixel { get; private set; }
        /// <summary>Number of colors per pixel in a packed image buffer.</summary>
        public int ColorValues { get; private set; }
        /// <summary>Number of 8 pixel blocks across.</summary>
        public int WidthInBlocks { get; private set; }
        /// <summary>Number of bytes that make up an 8 pixel block of a packed image buffer.</summary>
        public int BytesPerBlock { get; private set; }
        /// <summary>Number of bytes for an entire row of pixels for a full screen packed image buffer.</summary>
        public int FrameStride { get; private set; }
        /// <summary>Number of bytes for a full screen packed image buffer.</summary>
        public int FrameSize { get; private set; }
        /// <summary>Number of bytes for an entire row of pixels for a full screen unpacked image buffer (i.e., one byte per pixel).</summary>
        public int IntermediateFrameStride { get; private set; }
        /// <summary>Number of bytes for a full screen unpacked image buffer (i.e., one byte per pixel).</summary>
        public int IntermediateFrameSize { get; private set; }
        /// <summary>Bits flags for the various supported features of this badge.</summary>
        public SupportedFeatures SupportedFeatures { get; private set; }
        /// <summary>Communication rate of the connected badge.</summary>
        public int Baud { get; private set; }

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(this, obj))
            {
                return true;
            }

            BadgeCaps other = obj as BadgeCaps;
            if(other == null)
            {
                return false;
            }

            return
                (Width == other.Width) &&
                (Height == other.Height) &&
                (BitsPerPixel == other.BitsPerPixel) &&
                (SupportedFeatures == other.SupportedFeatures);
        }

        public override int GetHashCode()
        {
            return (int)((Width) | (Height << 8) | (BitsPerPixel << 16) | ((byte)SupportedFeatures << 24));
        }
    }

    /// <summary>
    /// Well known badges.
    /// </summary>
    public static class Badges
    {
        static Badges()
        {
            B1236 = new BadgeCaps(BadgeConnection.Version, 36, 12, 2, 0, 38400);
            B1248 = new BadgeCaps(BadgeConnection.Version, 48, 12, 2, SupportedFeatures.HardwareBrightness, 115200);
        }

        public static BadgeCaps B1236 { get; private set; }
        public static BadgeCaps B1248 { get; private set; }
    }
}
