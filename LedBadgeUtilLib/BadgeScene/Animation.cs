using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LedBadgeLib
{
    static class AnimHelper
    {
        public const float TimeEpsilon = 0.001f;

        public static int Round(float f)
        {
            /*return f > 0 ?
                (int)(f + 0.5f) :
                (int)(f - 0.5f);*/
            return (int)Math.Floor(f);
        }

        public static float Ease(float t, Easing ease)
        {
            if((ease == Easing.In || ease == Easing.Both) && t < 0.5f && t > 0.0f)
            {
                return (t * t) * 2;
            }
            else if((ease == Easing.Out || ease == Easing.Both) && t > 0.5f && t < 1.0f)
            {
                return 1 - ((t - 1) * (t - 1)) * 2;
            }

            return t;
        }
    }

    public enum SlidingDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    public enum Easing
    {
        None,
        In,
        Out,
        Both
    }

    public interface ITransitionController
    {
        MessageQueueItem Item1 { get; }
        MessageQueueItem Item2 { get; }
        bool IsFinished { get; }
        float OverTime { get; }
        void Update(float dt);
    }

    public interface IDisplayController
    {
        MessageQueueItem Item { get; }
        bool IsFinished { get; }
        float OverTime { get; }
        void Update(float dt);
    }

    public class NullTransition: ITransitionController
    {
        public NullTransition(
            MessageQueueItem item1,
            MessageQueueItem item2)
        {
            Item1 = item1;
            Item2 = item2;
        }

        public MessageQueueItem Item1 { get; private set; }
        public MessageQueueItem Item2 { get; private set; }
        public float OverTime { get { return 0; } }
        public bool IsFinished { get { return true; } }

        public void Update(float dt)
        {
        }
    }

    public class SlidingTransition: ITransitionController
    {
        public SlidingTransition(
            MessageQueueItem item1,
            MessageQueueItem item2,
            SlidingDirection direction,
            Easing ease,
            float speed,
            float? padding)
        {
            Item1 = item1;
            Item2 = item2;
            Direction = direction;
            Ease = ease;
            Speed = speed;
            if(IsVertical)
            {
                Padding = padding.HasValue ? padding.Value : BadgeCaps.Height;
                m_startOffset = Item1.Element.RenderY;
                m_distance = AnimHelper.Round(Item1.Element.ClipHeight + m_startOffset + Padding);
            }
            else
            {
                Padding = padding.HasValue ? padding.Value : BadgeCaps.Width;
                m_startOffset = Item1.Element.RenderX;
                m_distance = AnimHelper.Round(Item1.Element.ClipWidth + m_startOffset + Padding);
            }
            m_currOffset = m_startOffset;
            m_totalDuration = m_distance / Speed;
        }

        public MessageQueueItem Item1 { get; private set; }
        public MessageQueueItem Item2 { get; private set; }
        public SlidingDirection Direction { get; private set; }
        public Easing Ease { get; private set; }
        public float CurrentTime { get; private set; }
        public float Speed { get; private set; }
        public float Padding { get; private set; }
        public float OverTime { get { return CurrentTime - m_totalDuration; } }
        public bool IsFinished { get { return m_totalDuration - CurrentTime <= AnimHelper.TimeEpsilon; } }
        public bool IsVertical { get { return Direction == SlidingDirection.Up || Direction == SlidingDirection.Down; } }
        public bool IsForward { get { return Direction == SlidingDirection.Up || Direction == SlidingDirection.Left; } }

        public void Update(float dt)
        {
            CurrentTime += dt;
            float t = m_totalDuration > 0 ? CurrentTime / m_totalDuration : 0;
            t = t > 1 ? 1 : 
                t < 0 ? 0 :
                t;

            t = AnimHelper.Ease(t, Ease);

            if(IsForward)
            {
                m_currOffset = m_startOffset - t * m_distance;
            }
            else
            {
                m_currOffset = m_startOffset + t * m_distance;
            }

            if(IsVertical)
            {
                Item1.Element.RenderY = AnimHelper.Round(m_currOffset);
                Item2.Element.RenderY = AnimHelper.Round(Item1.Element.RenderY + Item1.Element.ClipHeight + Padding);
            }
            else
            {
                Item1.Element.RenderX = AnimHelper.Round(m_currOffset);
                Item2.Element.RenderX = AnimHelper.Round(Item1.Element.RenderX + Item1.Element.ClipWidth + Padding);
            }
        }

        float m_distance;
        float m_currOffset;
        float m_startOffset;
        float m_totalDuration;
    }

    public class TimedDisplay: IDisplayController
    {
        public TimedDisplay(
            MessageQueueItem item,
            float duration)
        {
            Item = item;
            Duration = duration;
        }
        
        public MessageQueueItem Item { get; private set; }
        public float Duration { get; private set; }
        public float CurrentTime { get; private set; }
        public float OverTime { get { return CurrentTime - Duration; } }
        public bool IsFinished { get { return Duration - CurrentTime <= AnimHelper.TimeEpsilon; } }
        
        public void Update(float dt)
        {
            CurrentTime += dt;
        }

        float m_currTime;
    }

    public class SlidingPosition: IDisplayController
    {
        public SlidingPosition(
            MessageQueueItem item,
            SlidingDirection direction,
            Easing ease,
            float speed,
            float padding)
        {
            Item = item;
            Direction = direction;
            Ease = ease;
            Speed = speed;
            Padding = padding;
            if(IsVertical)
            {
                m_distance = Item.Element.ClipHeight - BadgeCaps.Height;
            }
            else
            {
                m_distance = Item.Element.ClipWidth - BadgeCaps.Width;
            }
            m_distance += 2 * Padding;
            m_startOffset = Padding;
            m_currOffset = 0;
            m_totalDuration = m_distance / Speed;
        }

        public MessageQueueItem Item { get; private set; }
        public SlidingDirection Direction { get; private set; }
        public Easing Ease { get; private set; }
        public float CurrentTime { get; private set; }
        public float Speed { get; private set; }
        public float Padding { get; private set; }
        public float OverTime { get { return CurrentTime - m_totalDuration; } }
        public bool IsFinished { get { return m_totalDuration - CurrentTime <= AnimHelper.TimeEpsilon; } }
        public bool IsVertical { get { return Direction == SlidingDirection.Up || Direction == SlidingDirection.Down; } }
        public bool IsForward { get { return Direction == SlidingDirection.Up || Direction == SlidingDirection.Left; } }

        public void Update(float dt)
        {
            CurrentTime += dt;
            float t = m_totalDuration > 0 ? CurrentTime / m_totalDuration : 0;
            t = t > 1 ? 1 :
                t < 0 ? 0 :
                t;

            t = AnimHelper.Ease(t, Ease);

            if(IsForward)
            {
                m_currOffset = m_startOffset - t * m_distance;
            }
            else
            {
                m_currOffset = m_startOffset + t * m_distance;
            }

            if(IsVertical)
            {
                Item.Element.RenderY = AnimHelper.Round(m_currOffset);
            }
            else
            {
                Item.Element.RenderX =  AnimHelper.Round(m_currOffset);
            }
        }

        float m_distance;
        float m_currOffset;
        float m_startOffset;
        float m_totalDuration;
    }

    public class SlidingPosition2D: IDisplayController
    {
        public SlidingPosition2D(
            MessageQueueItem item,
            float directionX,
            float directionY,
            Easing ease,
            float speed,
            float paddingX,
            float paddingY)
        {
            Item = item;
            DirectionX = directionX == 0.0f ? 0 : directionX > 0.0f ? 1.0f : -1.0f;
            DirectionY = directionY == 0.0f ? 0 : directionY > 0.0f ? 1.0f : -1.0f;
            Ease = ease;
            Speed = speed;
            PaddingX = paddingX;
            PaddingY = paddingY;
            m_distanceX = (Item.Element.ClipWidth - BadgeCaps.Width) + 2 * PaddingX;
            m_distanceY = (Item.Element.ClipHeight - BadgeCaps.Height) + 2 * PaddingY;
            m_startOffsetX = PaddingX;
            m_startOffsetY = PaddingY;
            m_currOffsetX = 0;
            m_currOffsetY = 0;
            m_totalDuration = Math.Max(m_distanceX, m_distanceY) / Speed;
        }

        public MessageQueueItem Item { get; private set; }
        public float DirectionX { get; private set; }
        public float DirectionY { get; private set; }
        public Easing Ease { get; private set; }
        public float CurrentTime { get; private set; }
        public float Speed { get; private set; }
        public float PaddingX { get; private set; }
        public float PaddingY { get; private set; }
        public float OverTime { get { return CurrentTime - m_totalDuration; } }
        public bool IsFinished { get { return m_totalDuration - CurrentTime <= AnimHelper.TimeEpsilon; } }

        public void Update(float dt)
        {
            CurrentTime += dt;
            float t = m_totalDuration > 0 ? CurrentTime / m_totalDuration : 0;
            t = t > 1 ? 1 :
                t < 0 ? 0 :
                t;

            t = AnimHelper.Ease(t, Ease);

            m_currOffsetX = m_startOffsetX - t * DirectionX * m_distanceX;
            m_currOffsetY = m_startOffsetY - t * DirectionY * m_distanceY;

            Item.Element.RenderX = AnimHelper.Round(m_currOffsetX);
            Item.Element.RenderY = AnimHelper.Round(m_currOffsetY);
        }

        float m_distanceX;
        float m_distanceY;
        float m_currOffsetX;
        float m_currOffsetY;
        float m_startOffsetX;
        float m_startOffsetY;
        float m_totalDuration;
    }
}
