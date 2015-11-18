using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LedBadgeLib
{
    public class EmptyVisual: IBadgeVisual
    {
        public EmptyVisual(int width, int height)
        {
            ClipWidth = width;
            ClipHeight = height;
        }

        public int RenderX { get; set; }
        public int RenderY { get; set; }
        public int ClipX { get; set; }
        public int ClipY { get; set; }
        public int ClipWidth { get; set; }
        public int ClipHeight { get; set; }

        public void Update(float dt)
        {
        }

        public void Render(BadgeRenderTarget rt, int parentRenderX, int parentRenderY)
        {
        }
    }
}
