using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LedBadgeLib
{
    public static class BadgeImage
    {
        public static int CalculatePackedPixelBlocks(int widthInPixels)
        {
            return (widthInPixels + BadgeCaps.PixelsPerBlockBitPlane - 1) / BadgeCaps.PixelsPerBlockBitPlane;
        }

        public static int CalculatePackedPixelStride(int widthInPixels, PixelFormat pixelFormat)
        {
            return CalculatePackedPixelBlocks(widthInPixels) * (pixelFormat == PixelFormat.TwoBits ? 2 : 1);
        }

        public static int CalculatePackedBufferSize(int widthInPixels, int height, PixelFormat pixelFormat)
        {
            return CalculatePackedPixelStride(widthInPixels, pixelFormat) * height;
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

        public static Pix2x8 Pack8GrayPix(byte p0, byte p1, byte p2, byte p3, byte p4, byte p5, byte p6, byte p7)
        {
            p0 = SrgbGrayToPix(p0);
            p1 = SrgbGrayToPix(p1);
            p2 = SrgbGrayToPix(p2);
            p3 = SrgbGrayToPix(p3);
            p4 = SrgbGrayToPix(p4);
            p5 = SrgbGrayToPix(p5);
            p6 = SrgbGrayToPix(p6);
            p7 = SrgbGrayToPix(p7);

            ushort value = 0;
            
            value |= (ushort)((p0 & 0x01) << 0);
            value |= (ushort)((p1 & 0x01) << 1);
            value |= (ushort)((p2 & 0x01) << 2);
            value |= (ushort)((p3 & 0x01) << 3);
            value |= (ushort)((p4 & 0x01) << 4);
            value |= (ushort)((p5 & 0x01) << 5);
            value |= (ushort)((p6 & 0x01) << 6);
            value |= (ushort)((p7 & 0x01) << 7);

            value |= (ushort)((p0 & 0x02) << 7);
            value |= (ushort)((p1 & 0x02) << 8);
            value |= (ushort)((p2 & 0x02) << 9);
            value |= (ushort)((p3 & 0x02) << 10);
            value |= (ushort)((p4 & 0x02) << 11);
            value |= (ushort)((p5 & 0x02) << 12);
            value |= (ushort)((p6 & 0x02) << 13);
            value |= (ushort)((p7 & 0x02) << 14);

            return new Pix2x8(value);
        }

        public static void Unpack8GrayPix(Pix2x8 pix, out byte p0, out byte p1, out byte p2, out byte p3, out byte p4, out byte p5, out byte p6, out byte p7)
        {
            p0 = BadgeImage.PixToSrgbGray((byte)(((pix.Value & 0x0100) >>  7) | ((pix.Value & 0x01) >> 0)));
            p1 = BadgeImage.PixToSrgbGray((byte)(((pix.Value & 0x0200) >>  8) | ((pix.Value & 0x02) >> 1)));
            p2 = BadgeImage.PixToSrgbGray((byte)(((pix.Value & 0x0400) >>  9) | ((pix.Value & 0x04) >> 2)));
            p3 = BadgeImage.PixToSrgbGray((byte)(((pix.Value & 0x0800) >> 10) | ((pix.Value & 0x08) >> 3)));
            p4 = BadgeImage.PixToSrgbGray((byte)(((pix.Value & 0x1000) >> 11) | ((pix.Value & 0x10) >> 4)));
            p5 = BadgeImage.PixToSrgbGray((byte)(((pix.Value & 0x2000) >> 12) | ((pix.Value & 0x20) >> 5)));
            p6 = BadgeImage.PixToSrgbGray((byte)(((pix.Value & 0x4000) >> 13) | ((pix.Value & 0x40) >> 6)));
            p7 = BadgeImage.PixToSrgbGray((byte)(((pix.Value & 0x8000) >> 14) | ((pix.Value & 0x80) >> 7)));
        }

        public static void IntermediateImagetoPackedBuffer(byte[] intermediateImage, byte[] packedBuffer, PixelFormat pixelFormat, int offset, bool rotate)
        {
            int packedI = offset;
            if(rotate)
            {
                for(int p = intermediateImage.Length - 8; p >= 0; p -= 8)
                {
                    var pix = BadgeImage.Pack8GrayPix(
                        intermediateImage[p + 7],
                        intermediateImage[p + 6],
                        intermediateImage[p + 5],
                        intermediateImage[p + 4],
                        intermediateImage[p + 3],
                        intermediateImage[p + 2],
                        intermediateImage[p + 1],
                        intermediateImage[p + 0]);

                    packedBuffer[packedI++] = (byte)(pix.Value >> 8);
                    if(pixelFormat == PixelFormat.TwoBits)
                    {
                        packedBuffer[packedI++] = (byte)(pix.Value & 0xFF);
                    }
                }
            }
            else
            {
                for(int p = 0; p < intermediateImage.Length; p += 8)
                {
                    var pix = BadgeImage.Pack8GrayPix(
                        intermediateImage[p + 0],
                        intermediateImage[p + 1],
                        intermediateImage[p + 2],
                        intermediateImage[p + 3],
                        intermediateImage[p + 4],
                        intermediateImage[p + 5],
                        intermediateImage[p + 6],
                        intermediateImage[p + 7]);

                    packedBuffer[packedI++] = (byte)(pix.Value >> 8);
                    if(pixelFormat == PixelFormat.TwoBits)
                    {
                        packedBuffer[packedI++] = (byte)(pix.Value & 0xFF);
                    }
                }
            }
        }

        public static void PackedBufferToIntermediateImage(byte[] packedBuffer, byte[] intermediateImage, PixelFormat pixelFormat, int offset, bool rotate)
        {
            int packedI = offset;
            if(rotate)
            {
                for(int p = intermediateImage.Length - 8; p >= 0; p -= 8)
                {
                    ushort value = packedBuffer[packedI++];
                    value = (ushort)((value << 8) | ((pixelFormat == PixelFormat.TwoBits) ? packedBuffer[packedI++] : value));

                    Unpack8GrayPix(new Pix2x8(value),
                        out intermediateImage[p + 7],
                        out intermediateImage[p + 6],
                        out intermediateImage[p + 5],
                        out intermediateImage[p + 4],
                        out intermediateImage[p + 3],
                        out intermediateImage[p + 2],
                        out intermediateImage[p + 1],
                        out intermediateImage[p + 0]);
                }
            }
            else
            {
                for(int p = 0; p < intermediateImage.Length; p += 8)
                {
                    ushort value = packedBuffer[packedI++];
                    value = (ushort)((value << 8) | ((pixelFormat == PixelFormat.TwoBits) ? packedBuffer[packedI++] : value));

                    Unpack8GrayPix(new Pix2x8(value),
                        out intermediateImage[p + 0],
                        out intermediateImage[p + 1],
                        out intermediateImage[p + 2],
                        out intermediateImage[p + 3],
                        out intermediateImage[p + 4],
                        out intermediateImage[p + 5],
                        out intermediateImage[p + 6],
                        out intermediateImage[p + 7]);
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

                    int withError = p; //p > (255 - 32) ? 255 : p + 32;
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
