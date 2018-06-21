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
    public class RawVisual: IBadgeVisual
    {
        public RawVisual(int width, int height, bool enableBlend)
        {
            ClipWidth = width;
            ClipHeight = height;

            m_cachedIntermediate = new BadgeRenderTarget(ClipWidth, ClipHeight, PixelFormat.TwoBits);
            if(enableBlend)
            {
                m_alphaMask = new byte[m_cachedIntermediate.IntermediateImage.Length];
            }
        }

        public RawVisual(int width, int height, IBadgeVisual bake)
        {
            ClipWidth = width;
            ClipHeight = height;

            m_cachedIntermediate = new BadgeRenderTarget(ClipWidth, ClipHeight, PixelFormat.TwoBits);
            bake.Render(m_cachedIntermediate, 0, 0);
        }

        public int RenderX { get; set; }
        public int RenderY { get; set; }
        public int ClipX { get; set; }
        public int ClipY { get; set; }
        public int ClipWidth { get; set; }
        public int ClipHeight { get; set; }

        public int Width { get { return m_cachedIntermediate.WidthInPixels; } }
        public int Height { get { return m_cachedIntermediate.Height; } }
        public byte[] IntermediateImage { get { return m_cachedIntermediate.IntermediateImage; } }
        public byte[] AlphaMask { get { return m_alphaMask; } }
        public bool EnableBlend { get { return m_alphaMask != null; } }

        public void Update(float dt)
        {
        }

        public void Render(BadgeRenderTarget rt, int parentRenderX, int parentRenderY)
        {
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

        BadgeRenderTarget m_cachedIntermediate;
        byte[] m_alphaMask;
    }
}
