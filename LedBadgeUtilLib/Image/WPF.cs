using System;
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
        public static void ReadImage(byte[] intermediateImage, BitmapSource src, int srcX, int srcY, int width, int height)
        {
            if(src.Format == PixelFormats.Gray8)
            {
                ReadGrayImage(intermediateImage, src, srcX, srcY, width, height);
            }
            else
            {
                Read32BitImage(intermediateImage, src, srcX, srcY, width, height);
            }
        }

        public static void Read32BitImage(byte[] intermediateImage, BitmapSource src, int srcX, int srcY, int width, int height)
        {
            System.Diagnostics.Debug.Assert(src.Format.BitsPerPixel == 32);

            int[] pix = new int[width * height];
            src.CopyPixels(new Int32Rect(srcX, srcY, width, height), pix, width * sizeof(int), 0);

            for(int i = 0; i < pix.Length; ++i)
            {
                int p = pix[i];
                intermediateImage[i] = BadgeImage.ToGray((p >> 16) & 0xFF, (p >> 8) & 0xFF, (p >> 0) & 0xFF);
            }
        }

        public static void Read32BitImage(byte[] intermediateImage, byte[] alphaMask, BitmapSource src, int srcX, int srcY, int width, int height)
        {
            System.Diagnostics.Debug.Assert(src.Format.BitsPerPixel == 32);

            int[] pix = new int[width * height];
            src.CopyPixels(new Int32Rect(srcX, srcY, width, height), pix, width * sizeof(int), 0);

            for(int i = 0; i < pix.Length; ++i)
            {
                int p = pix[i];
                intermediateImage[i] = BadgeImage.ToGray((p >> 16) & 0xFF, (p >> 8) & 0xFF, (p >> 0) & 0xFF);
                alphaMask[i] = (byte)(p >> 24);
            }
        }

        public static void ReadGrayImage(byte[] intermediateImage, BitmapSource src, int srcX, int srcY, int width, int height)
        {
            System.Diagnostics.Debug.Assert(src.Format == PixelFormats.Gray8);

            src.CopyPixels(new Int32Rect(srcX, srcY, width, height), intermediateImage, width * sizeof(byte), 0);
        }

        public static Color ColorFromPix(byte value)
        {
            byte g = BadgeImage.PixToSrgbGray(value);
            return Color.FromRgb(g, g, g);
        }

        public static BitmapSource ImageFromIntermediate(byte[] intermediateImage, int width, int height)
        {
            var image = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, null);
            ImageFromIntermediate(image, intermediateImage, width, height);
            return image;
        }

        public static void ImageFromIntermediate(WriteableBitmap target, byte[] intermediateImage, int width, int height)
        {
            target.WritePixels(new Int32Rect(0, 0, width, height), intermediateImage, width, 0);
        }

        public static BitmapSource ImageFromPackedBuffer(byte[] packedBuffer, int offset, bool rotate, int width, int height)
        {
            var image = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, null);
            ImageFromPackedBuffer(image, packedBuffer, offset, rotate, width, height);
            return image;
        }

        public static void ImageFromPackedBuffer(WriteableBitmap target, byte[] packedBuffer, int offset, bool rotate, int width, int height, byte[] tempIntermediate = null)
        {
            var intermediateImage = tempIntermediate ?? new byte[width * height];
            BadgeImage.PackedBufferToIntermediateImage(packedBuffer, intermediateImage, offset, rotate);
            ImageFromIntermediate(target, intermediateImage, width, height);
        }

        public static FrameworkElement MakeSingleLineItem(string message, bool halfSize = false, bool fullWidth = true)
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
                MinWidth = !fullWidth || halfSize ? 0 : BadgeCaps.Width,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };
            TextOptions.SetTextFormattingMode(element, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(element, TextRenderingMode.Aliased);
            TextOptions.SetTextHintingMode(element, TextHintingMode.Fixed);

            return element;
        }

        public static FrameworkElement MakeDoubleLineItem(string message1, string message2, bool fullWidth = true)
        {
            var element1 = MakeSingleLineItem(message1, halfSize: true, fullWidth: fullWidth);
            var element2 = MakeSingleLineItem(message2, halfSize: true, fullWidth: fullWidth);

            var element = new StackPanel();
            element.Children.Add(element1);
            element.Children.Add(element2);

            return element;
        }

        public static FrameworkElement MakeSplitLineItem(string message1, string message2, string message3, bool fullWidth = true)
        {
            var element1 = MakeSingleLineItem(message1, fullWidth: false);
            var element2 = MakeDoubleLineItem(message2, message3, fullWidth: false);

            var element = new StackPanel() 
            { 
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                MinWidth = !fullWidth ? 0 : BadgeCaps.Width
            };
            element.Children.Add(element1);
            element.Children.Add(element2);

            return element;
        }

        public static MessageQueueItem MakeQueuedItem(FrameworkElement element)
        {
            return new MessageQueueItem(new WpfVisual(element));
        }
    }
}
