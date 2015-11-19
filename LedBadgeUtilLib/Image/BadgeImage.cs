using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LedBadgeLib
{
    public static class BadgeImage
    {
        public static int CalculatePackedBufferSize(int width, int height)
        {
            return (width * height + BadgeCaps.PixelsPerByte - 1) / BadgeCaps.PixelsPerByte;
        }

        public static byte ToGray(int r, int g, int b)
        {
            return (byte)((r * 2126 + g * 7152 + b * 0722) / 10000);
        }

        public static byte SrgbGrayToPix(byte g)
        {
            return (byte)(g >> 6);
        }

        public static byte PixToSrgbGray(byte p)
        {
            switch(p & 0x3)
            {
                case 0:  return 0;
                case 1:  return 127;
                case 2:  return 191;
                default: return 255;
            }
        }

        public static byte Pack4RGBPix(int p0, int p1, int p2, int p3)
        {
            return Pack4GrayPix(
                ToGray((p0 >> 16) & 0xFF, (p0 >> 8) & 0xFF, (p0 >> 0) & 0xFF),
                ToGray((p1 >> 16) & 0xFF, (p1 >> 8) & 0xFF, (p1 >> 0) & 0xFF),
                ToGray((p2 >> 16) & 0xFF, (p2 >> 8) & 0xFF, (p2 >> 0) & 0xFF),
                ToGray((p3 >> 16) & 0xFF, (p3 >> 8) & 0xFF, (p3 >> 0) & 0xFF));
        }

        public static byte Pack4GrayPix(byte p0, byte p1, byte p2, byte p3)
        {
            byte dst = 0;
            dst |= (byte)(SrgbGrayToPix(p0) << 0);
            dst |= (byte)(SrgbGrayToPix(p1) << 2);
            dst |= (byte)(SrgbGrayToPix(p2) << 4);
            dst |= (byte)(SrgbGrayToPix(p3) << 6);
            return dst;
        }

        public static void IntermediateImagetoPackedBuffer(byte[] intermediateImage, byte[] packedBuffer, int offset, bool rotate)
        {
            int packedI = offset;
            if(rotate)
            {
                for(int p = intermediateImage.Length - 4; p >= 0; ++packedI, p -= 4)
                {
                    packedBuffer[packedI] = BadgeImage.Pack4GrayPix(
                        intermediateImage[p + 3],
                        intermediateImage[p + 2],
                        intermediateImage[p + 1],
                        intermediateImage[p + 0]);
                }
            }
            else
            {
                for(int p = 0; p < intermediateImage.Length; ++packedI, p += 4)
                {
                    packedBuffer[packedI] = BadgeImage.Pack4GrayPix(
                        intermediateImage[p + 0],
                        intermediateImage[p + 1],
                        intermediateImage[p + 2],
                        intermediateImage[p + 3]);
                }
            }
        }

        public static void PackedBufferToIntermediateImage(byte[] packedBuffer, byte[] intermediateImage, int offset, bool rotate)
        {
            int packedI = offset;
            if(rotate)
            {
                for(int p = intermediateImage.Length - 4; p >= 0; ++packedI, p -= 4)
                {
                    intermediateImage[p + 3] = BadgeImage.PixToSrgbGray((byte)((packedBuffer[packedI] >> 0) & 0x3));
                    intermediateImage[p + 2] = BadgeImage.PixToSrgbGray((byte)((packedBuffer[packedI] >> 2) & 0x3));
                    intermediateImage[p + 1] = BadgeImage.PixToSrgbGray((byte)((packedBuffer[packedI] >> 4) & 0x3));
                    intermediateImage[p + 0] = BadgeImage.PixToSrgbGray((byte)((packedBuffer[packedI] >> 6) & 0x3));
                }
            }
            else
            {
                for(int p = 0; p < intermediateImage.Length; ++packedI, p += 4)
                {
                    intermediateImage[p + 0] = BadgeImage.PixToSrgbGray((byte)((packedBuffer[packedI] >> 0) & 0x3));
                    intermediateImage[p + 1] = BadgeImage.PixToSrgbGray((byte)((packedBuffer[packedI] >> 2) & 0x3));
                    intermediateImage[p + 2] = BadgeImage.PixToSrgbGray((byte)((packedBuffer[packedI] >> 4) & 0x3));
                    intermediateImage[p + 3] = BadgeImage.PixToSrgbGray((byte)((packedBuffer[packedI] >> 6) & 0x3));
                }
            }
        }

        public static bool ClipRects(
            int targetWidth, int targetHeight, int sourceWidth, int sourceHeight,
            ref int destX, ref int destY, ref int srcX, ref int srcY, ref int width, ref int height)
        {
            int deltaLeft = srcX < 0 ? -srcX : 0;
            int deltaTop = srcY < 0 ? -srcY : 0;
            int deltaRight = (srcX + width) > sourceWidth ? (srcX + width) - sourceWidth : 0;
            int deltaBottom = (srcY + height) > sourceHeight ? (srcY + height) - sourceHeight : 0;

            srcX += deltaLeft;
            destX += deltaLeft;
            width -= deltaLeft;
            srcY += deltaTop;
            destY += deltaTop;
            height -= deltaTop;
            width -= deltaRight;
            height -= deltaBottom;

            deltaLeft = destX < 0 ? -destX : 0;
            deltaTop = destY < 0 ? -destY : 0;
            deltaRight = (destX + width) > targetWidth ? (destX + width) - targetWidth : 0;
            deltaBottom = (destY + height) > targetHeight ? (destY + height) - targetHeight : 0;

            srcX += deltaLeft;
            destX += deltaLeft;
            width -= deltaLeft;
            srcY += deltaTop;
            destY += deltaTop;
            height -= deltaTop;
            width -= deltaRight;
            height -= deltaBottom;

            return width > 0 && height > 0;
        }

        public static void Blit(
            byte[] intermediateImageTarget, int targetWidth, int targetHeight,
            byte[] intermediateImageSource, int sourceWidth, int sourceHeight,
            int destX, int destY, int srcX, int srcY, int width, int height)
        {
            if(intermediateImageTarget.Length == intermediateImageSource.Length &&
                targetWidth == sourceWidth &&
                targetHeight == sourceHeight &&
                targetWidth == width &&
                targetHeight == height &&
                destX == 0 &&
                destY == 0 &&
                srcX == 0 &&
                srcY == 0)
            {
                Array.Copy(intermediateImageSource, intermediateImageTarget, intermediateImageSource.Length);
            }
            else if(ClipRects(targetWidth, targetHeight, sourceWidth, sourceHeight, ref destX, ref destY, ref srcX, ref srcY, ref width, ref height))
            {
                int s0 = srcY * sourceWidth + srcX;
                int d0 = destY * targetWidth + destX;
                for(int y = 0; y < height; ++y)
                {
                    int si = s0;
                    int di = d0;
                    for(int x = 0; x < width; ++x)
                    {
                        intermediateImageTarget[di++] = intermediateImageSource[si++];
                    }
                    s0 += sourceWidth;
                    d0 += targetWidth;
                }
            }
        }

        public static void Blit(
            byte[] intermediateImageTarget, int targetWidth, int targetHeight,
            byte[] intermediateImageSource, byte[] sourceAlphaMask, int sourceWidth, int sourceHeight,
            int destX, int destY, int srcX, int srcY, int width, int height)
        {
            if(ClipRects(targetWidth, targetHeight, sourceWidth, sourceHeight, ref destX, ref destY, ref srcX, ref srcY, ref width, ref height))
            {
                int s0 = srcY * sourceWidth + srcX;
                int d0 = destY * targetWidth + destX;
                for(int y = 0; y < height; ++y)
                {
                    int si = s0;
                    int di = d0;
                    for(int x = 0; x < width; ++x, ++di, ++si)
                    {
                        byte sa = sourceAlphaMask[si];
                        if(sa == 255)
                        {
                            intermediateImageTarget[di] = intermediateImageSource[si];
                        }
                        else if(sa > 0)
                        { 
                            // assumes premultiplied...
                            intermediateImageTarget[di] = (byte)((intermediateImageTarget[di] * (255 - sa) + intermediateImageSource[si]) / 255);
                        }
                    }
                    s0 += sourceWidth;
                    d0 += targetWidth;
                }
            }
        }

        public static void DitherImage(byte[] intermediateImage, int width, int height)
        {
            for(int y = 0, srcI = 0; y < height; ++y)
            {
                for(int x = 0; x < width; ++x, ++srcI)
                {
                    int p = intermediateImage[srcI];

                    int withError = p;//p > (255 - 32) ? 255 : p + 32;
                    byte rounded = BadgeImage.PixToSrgbGray(BadgeImage.SrgbGrayToPix((byte)withError));
                    intermediateImage[srcI] = rounded;

                    int newError = withError - rounded;
                    int scaledError = (newError << 10) / 42;

                    for(int ei = 0; ei < s_StuckiPattern.Length; ++ei)
                    {
                        var e = s_StuckiPattern[ei];
                        int ex = x + e.Item1;
                        int ey = y + e.Item2;
                        if(ex >= 0 && ey >= 0 && ex < width && ey < height)
                        {
                            int weightedError = (scaledError * e.Item3) >> 10;
                            int diffusedIndex = ey * width + ex;

                            int newValue = intermediateImage[diffusedIndex] + weightedError;
                            if(newValue < 0)
                            {
                                intermediateImage[diffusedIndex] = 0;
                            }
                            else if(newValue > 255)
                            {
                                intermediateImage[diffusedIndex] = 255;
                            }
                            else
                            {
                                intermediateImage[diffusedIndex] = (byte)newValue;
                            }
                        }
                    }
                }
            }
        }
        static Tuple<int, int, int>[] s_StuckiPattern = new Tuple<int, int, int>[]
        {
            /*        -2        */  /*        -1        */  /*        0        */  Tuple.Create(1, 0, 8), Tuple.Create(2, 0, 4), 
            Tuple.Create(-2, 1, 2), Tuple.Create(-1, 1, 4), Tuple.Create(0, 1, 8), Tuple.Create(1, 1, 4), Tuple.Create(2, 1, 2),
            Tuple.Create(-2, 2, 1), Tuple.Create(-1, 2, 2), Tuple.Create(0, 2, 4), Tuple.Create(1, 2, 2), Tuple.Create(2, 2, 1)
        };
    }
}
