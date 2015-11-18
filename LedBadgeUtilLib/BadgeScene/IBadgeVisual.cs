using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LedBadgeLib
{
    public interface IBadgeVisual
    {
        void Update(float dt);
        void Render(BadgeRenderTarget rt, int parentRenderX, int parentRenderY);

        int RenderX { get; set; }
        int RenderY { get; set; }
        int ClipX { get; set; }
        int ClipY { get; set; }
        int ClipWidth { get; set; }
        int ClipHeight { get; set; }
    }
}
