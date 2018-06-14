using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LedBadgeLib
{
    public class BadgeRenderTarget
    {
        public BadgeRenderTarget(int widthInPixels, int height, PixelFormat packedFormat, byte[] intermediateImage = null)
        {
            WidthInBlocks = BadgeImage.CalculatePackedPixelBlocks(widthInPixels);
            WidthInPixels = WidthInBlocks * BadgeCaps.PixelsPerBlockBitPlane;
            Height = height;
            PackedFormat = packedFormat;
            IntermediateImage = intermediateImage ?? new byte[WidthInPixels * height];
        }

        public void DitherImage()
        {
            BadgeImage.DitherImage(IntermediateImage, WidthInPixels, Height);
        }

        public void PackBuffer(bool rotate)
        {
            if(PackedBuffer == null)
            {
                PackedBuffer = new byte[BadgeImage.CalculatePackedBufferSize(WidthInPixels, Height, PackedFormat)];
            }
            BadgeImage.IntermediateImagetoPackedBuffer(IntermediateImage, PackedBuffer, PackedFormat, 0, rotate);
        }

        public bool SameDimentions(int widthInPixels, int height, PixelFormat packedFormat)
        {
            return
                PackedFormat == packedFormat &&
                Height == height &&
                WidthInBlocks == BadgeImage.CalculatePackedPixelBlocks(widthInPixels);
        }

        public int WidthInBlocks { get; private set; }
        public int WidthInPixels { get; private set; }
        public int Height { get; private set; }
        public PixelFormat PackedFormat { get; private set; }
        public byte[] IntermediateImage { get; set; }
        public byte[] PackedBuffer { get; set; }
    }
}
