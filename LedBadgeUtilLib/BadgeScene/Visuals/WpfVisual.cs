using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;

namespace LedBadgeLib
{
    public class WpfVisual: IBadgeVisual
    {
        public WpfVisual(BadgeCaps device, FrameworkElement element, int defaultWidth = -1, int defaultHeight = -1, bool dither = false, bool enableBlend = false)
        {
            if(defaultWidth <= 0) { defaultWidth = device.Width; }
            if(defaultHeight <= 0) { defaultHeight = device.Height; }

            Element = element;
            Element.Measure(new Size(defaultWidth, defaultHeight));
            Element.Arrange(new Rect(0, 0, defaultWidth, defaultHeight));

            ClipWidth = (int)Math.Ceiling(Element.ActualWidth);
            ClipHeight = (int)Math.Ceiling(Element.ActualHeight);

            Dither = dither;
            EnableBlend = enableBlend;

            m_cachedIntermediate = new BadgeRenderTarget(ClipWidth, ClipHeight, PixelFormat.TwoBits);
            m_renderTarget = new RenderTargetBitmap(ClipWidth, ClipHeight, 96, 96, PixelFormats.Pbgra32);

            Update(0); // To avoid remeasuring on a background thread
            UpdateCachedImage();
        }

        public int RenderX { get; set; }
        public int RenderY { get; set; }
        public int ClipX { get; set; }
        public int ClipY { get; set; }
        public int ClipWidth { get; set; }
        public int ClipHeight { get; set; }

        public bool Dither { get; set; }
        public bool EnableBlend { get; set; }

        public FrameworkElement Element 
        {
            get { return m_element; }
            set
            {
                if(m_element != value)
                {
                    m_element = value;
                    DirtyVisual();
                }
            }
        }

        public void DirtyVisual()
        {
            m_visualLayoutDirty = true;
            m_visualRenderDirty = true;
        }

        public void Update(float dt)
        {
            if(m_visualLayoutDirty)
            {
                m_visualLayoutDirty = false;
                
                Element.Measure(new Size(ClipWidth, ClipHeight));
                Element.Arrange(new Rect(0, 0, ClipWidth, ClipHeight));
            }
        }

        public void Render(BadgeRenderTarget rt, int parentRenderX, int parentRenderY)
        {
            UpdateCachedImage();

            if(EnableBlend)
            {
                BadgeImage.Blit(
                    rt.IntermediateImage, rt.IntermediateStride, rt.WidthInPixels, rt.Height,
                    m_cachedIntermediate.IntermediateImage, m_alphaMask, m_cachedIntermediate.IntermediateStride, m_cachedIntermediate.WidthInPixels, m_cachedIntermediate.Height,
                    parentRenderX + RenderX, parentRenderY + RenderY, ClipX, ClipY, ClipWidth, ClipHeight);
            }
            else
            {
                BadgeImage.Blit(
                    rt.IntermediateImage, rt.IntermediateStride, rt.WidthInPixels, rt.Height,
                    m_cachedIntermediate.IntermediateImage, m_cachedIntermediate.IntermediateStride, m_cachedIntermediate.WidthInPixels, m_cachedIntermediate.Height,
                    parentRenderX + RenderX, parentRenderY + RenderY, ClipX, ClipY, ClipWidth, ClipHeight);
            }
        }

        private void UpdateCachedImage()
        {
            if(m_visualRenderDirty)
            {
                m_visualRenderDirty = false;

                m_renderTarget.Clear();
                m_renderTarget.Render(Element);

                if(EnableBlend)
                {
                    if(m_alphaMask == null)
                    {
                        m_alphaMask = new byte[m_cachedIntermediate.IntermediateImage.Length];
                    }
                    WPF.Read32BitImage(m_cachedIntermediate.IntermediateImage, m_alphaMask, m_renderTarget, 0, 0, m_cachedIntermediate.IntermediateStride, m_cachedIntermediate.WidthInPixels, m_cachedIntermediate.Height);
                }
                else
                {
                    m_alphaMask = null;
                    WPF.Read32BitImage(m_cachedIntermediate.IntermediateImage, m_renderTarget, 0, 0, m_cachedIntermediate.IntermediateStride, m_cachedIntermediate.WidthInPixels, m_cachedIntermediate.Height);
                    if(Dither)
                    {
                        m_cachedIntermediate.DitherImage();
                    }
                }
            }
        }

        bool m_visualRenderDirty;
        bool m_visualLayoutDirty;
        FrameworkElement m_element;
        RenderTargetBitmap m_renderTarget;
        BadgeRenderTarget m_cachedIntermediate;
        byte[] m_alphaMask;
    }
}
