using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LedBadgeLib
{
    public class BadgeRenderTarget
    {
        public BadgeRenderTarget(int width = BadgeCaps.Width, int height = BadgeCaps.Height, byte[] intermediateImage = null)
        {
            Width = width;
            Height = height;
            IntermediateImage = intermediateImage ?? new byte[width * height];
        }

        public void DitherImage()
        {
            BadgeImage.DitherImage(IntermediateImage, Width, Height);
        }

        public void PackBuffer(bool rotate)
        {
            if(PackedBuffer == null)
            {
                System.Diagnostics.Debug.Assert((Width & 0x3) == 0);
                PackedBuffer = new byte[(Width * BadgeCaps.BitsPerPixel / 8) * Height];
            }
            BadgeImage.IntermediateImagetoPackedBuffer(IntermediateImage, PackedBuffer, Width, Height, 0, rotate);
        }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public byte[] IntermediateImage { get; set; }
        public byte[] PackedBuffer { get; set; }
    }
}
