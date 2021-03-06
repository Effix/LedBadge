﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LedBadgeLib
{
    public static class WPF
    {
        public static void ReadImage(byte[] intermediateImage, BitmapSource src, int srcX, int srcY, int stride, int width, int height)
        {
            if(src.Format == PixelFormats.Gray8)
            {
                ReadGrayImage(intermediateImage, src, srcX, srcY, stride, width, height);
            }
            else
            {
                Read32BitImage(intermediateImage, src, srcX, srcY, stride, width, height);
            }
        }

        public static void Read32BitImage(byte[] intermediateImage, BitmapSource src, int srcX, int srcY, int stride, int width, int height)
        {
            System.Diagnostics.Debug.Assert(src.Format.BitsPerPixel == 32);

            int[] pix = new int[width * height];
            src.CopyPixels(new Int32Rect(srcX, srcY, width, height), pix, width * sizeof(int), 0);

            int si = 0;
            int di0 = 0;
            for(int y = 0; y < height; ++y, di0 += stride)
            {
                int di = di0;
                for(int x = 0; x < width; ++x, ++si, ++di)
                {
                    int p = pix[si];
                    intermediateImage[di] = BadgeImage.ToGray((p >> 16) & 0xFF, (p >> 8) & 0xFF, (p >> 0) & 0xFF);
                }
            }
        }

        public static void Read32BitImage(byte[] intermediateImage, byte[] alphaMask, BitmapSource src, int srcX, int srcY, int stride, int width, int height)
        {
            System.Diagnostics.Debug.Assert(src.Format.BitsPerPixel == 32);

            int[] pix = new int[width * height];
            src.CopyPixels(new Int32Rect(srcX, srcY, width, height), pix, width * sizeof(int), 0);

            int si = 0;
            int di0 = 0;
            for(int y = 0; y < height; ++y, di0 += stride)
            {
                int di = di0;
                for(int x = 0; x < width; ++x, ++si, ++di)
                {
                    int p = pix[si];
                    intermediateImage[di] = BadgeImage.ToGray((p >> 16) & 0xFF, (p >> 8) & 0xFF, (p >> 0) & 0xFF);
                    alphaMask[di] = (byte)(p >> 24);
                }
            }
        }

        public static void ReadGrayImage(byte[] intermediateImage, BitmapSource src, int srcX, int srcY, int stride, int width, int height)
        {
            System.Diagnostics.Debug.Assert(src.Format == PixelFormats.Gray8);

            src.CopyPixels(new Int32Rect(srcX, srcY, width, height), intermediateImage, stride, 0);
        }

        public static Color ColorFromPix(byte value)
        {
            byte g = BadgeImage.PixToSrgbGray(value);
            return Color.FromRgb(g, g, g);
        }

        public static BitmapSource ImageFromIntermediate(byte[] intermediateImage, int stride, int width, int height)
        {
            var image = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, null);
            ImageFromIntermediate(image, intermediateImage, stride, width, height);
            return image;
        }

        public static void ImageFromIntermediate(WriteableBitmap target, byte[] intermediateImage, int stride, int width, int height)
        {
            target.WritePixels(new Int32Rect(0, 0, width, height), intermediateImage, stride, 0);
        }

        public static BitmapSource ImageFromPackedBuffer(byte[] packedBuffer, int offset, bool rotate, int stride, int width, int height, PixelFormat pixelFormat)
        {
            if(width > 0 && height > 0)
            {
                var image = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, null);
                ImageFromPackedBuffer(image, packedBuffer, offset, rotate, stride, width, height, pixelFormat);
                return image;
            }
            return null;
        }

        public static void ImageFromPackedBuffer(WriteableBitmap target, byte[] packedBuffer, int offset, bool rotate, int stride, int width, int height, PixelFormat pixelFormat, byte[] tempIntermediate = null)
        {
            var intermediateImage = tempIntermediate ?? new byte[stride * height];
            BadgeImage.PackedBufferToIntermediateImage(packedBuffer, intermediateImage, pixelFormat, offset, rotate);
            ImageFromIntermediate(target, intermediateImage, stride, width, height);
        }

        public static FrameworkElement MakeSingleLineItem(BadgeCaps device, string message, bool halfSize = false, bool fullWidth = true)
        {
            var size = halfSize ? 7 : 12;
            var font = new FontFamily(halfSize ? "Lucida Console" : "Arial");
            Brush color = Brushes.White;
            var element = new TextBlock()
            {
                Text = message,
                Background = Brushes.Transparent,
                FontSize = size,
                Margin = halfSize ? new Thickness(0, 0, 0, -1) : new Thickness(0, -2, 0, 0),
                FontFamily = font,
                TextWrapping = TextWrapping.NoWrap,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                TextAlignment = System.Windows.TextAlignment.Center,
                Foreground = color,
                MinWidth = !fullWidth || halfSize ? 0 : device.Width,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };
            TextOptions.SetTextFormattingMode(element, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(element, TextRenderingMode.Aliased);
            TextOptions.SetTextHintingMode(element, TextHintingMode.Fixed);

            return element;
        }

        public static FrameworkElement MakeDoubleLineItem(BadgeCaps device, string message1, string message2, bool fullWidth = true)
        {
            var element1 = MakeSingleLineItem(device, message1, halfSize: true, fullWidth: fullWidth);
            var element2 = MakeSingleLineItem(device, message2, halfSize: true, fullWidth: fullWidth);

            var element = new StackPanel();
            element.Children.Add(element1);
            element.Children.Add(element2);

            return element;
        }

        public static FrameworkElement MakeSplitLineItem(BadgeCaps device, string message1, string message2, string message3, bool fullWidth = true)
        {
            var element1 = MakeSingleLineItem(device, message1, fullWidth: false);
            var element2 = MakeDoubleLineItem(device, message2, message3, fullWidth: false);

            var element = new StackPanel() 
            { 
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                MinWidth = !fullWidth ? 0 : device.Width
            };
            element.Children.Add(element1);
            element.Children.Add(element2);

            return element;
        }

        public static MessageQueueItem MakeQueuedItem(BadgeCaps device, FrameworkElement element)
        {
            return new MessageQueueItem(new WpfVisual(device, element));
        }
    }
}
