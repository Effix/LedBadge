using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LedBadgeLib
{
    public class MessageQueueVisual: IBadgeVisual
    {
        public MessageQueueVisual(MessageQueue queue = null)
        {
            Queue = queue ?? new MessageQueue();
            ClipWidth = BadgeCaps.Width;
            ClipHeight = BadgeCaps.Height;
        }

        public int RenderX { get; set; }
        public int RenderY { get; set; }
        public int ClipX { get; set; }
        public int ClipY { get; set; }
        public int ClipWidth { get; set; }
        public int ClipHeight { get; set; }

        public MessageQueue Queue { get; set; }

        public delegate ITransitionController GetTransitionDelegate(MessageQueueItem fromItem, MessageQueueItem toItem);
        public GetTransitionDelegate GetTransition { get; set; }

        public delegate IDisplayController GetDisplayDelegate(MessageQueueItem nextItem);
        public GetDisplayDelegate GetDisplay { get; set; }

        public delegate bool ExhaustedQueueDelegate(MessageQueue queue, MessageQueueItem lastItem);
        public ExhaustedQueueDelegate ExhaustedQueue { get; set; }

        public void Render(BadgeRenderTarget rt, int parentRenderX, int parentRenderY)
        {
            if(m_currentItem != null)
            {
                m_currentItem.Element.Render(rt, parentRenderX + RenderX, parentRenderY + RenderY);
            }
            else if(m_currentTransition != null)
            {
                m_currentTransition.Item1.Element.Render(rt, parentRenderX + RenderX, parentRenderY + RenderY);
                m_currentTransition.Item2.Element.Render(rt, parentRenderX + RenderX, parentRenderY + RenderY);
            }
        }

        public void Update(float dt)
        {
            if(m_currentTransition != null)
            {
                // step the transition
                m_currentTransition.Item1.Element.Update(dt);
                m_currentTransition.Item2.Element.Update(dt);
                UpdateTransition(dt);
            }
            else if(m_currentItem != null)
            {
                // step the current item
                m_currentItem.Element.Update(dt);
                m_currentItem.DisplayController.Update(dt);
                if(m_currentItem.DisplayController.IsFinished)
                {
                    float remTime = m_currentItem.DisplayController.OverTime;

                    // transition to the next item, if there is one
                    var nextItem = GetNext();
                    if(nextItem != null)
                    {
                        nextItem.Element.Update(remTime);
                        BeginTransition(m_currentItem, nextItem, remTime);
                    }
                }
            }
            else
            {
                // no item of transition is active? better get one...
                var nextItem = GetNext();
                if(nextItem != null)
                {
                    nextItem.Element.Update(0.0f);
                    var empty = new MessageQueueItem(new EmptyVisual(ClipWidth, ClipHeight));
                    BeginTransition(empty, nextItem, 0);
                }
            }
        }

        void BeginTransition(MessageQueueItem from, MessageQueueItem to, float initialStep)
        {
            m_currentTransition = GetTransitionController(from, to);
            m_currentItem = null;

            UpdateTransition(initialStep);
        }

        void UpdateTransition(float dt)
        {
            m_currentTransition.Update(dt);
            if(m_currentTransition.IsFinished)
            {
                float remTime = m_currentTransition.OverTime;

                // end the transition
                m_currentItem = m_currentTransition.Item2;
                m_currentTransition = null;

                m_currentItem.DisplayController.Update(remTime);
            }
        }

        MessageQueueItem GetNext()
        {
            MessageQueueItem message;

            for(;;)
            {
                message = Queue.Dequeue();
                if(message == null && RaiseMessageQueueExhauseted())
                {
                    message = Queue.Dequeue();
                }
                if(message == null)
                {
                    break;
                }

                //Debug.Assert(message.State == QueueState.Queued || message.State == QueueState.Removed);
                if(message.State != QueueState.Removed)
                {
                    //message.State = QueueState.Displaying;
                    if(message.DisplayController == null)
                    {
                        message.DisplayController = GetDisplayController(message);
                    }

                    break;
                }
            }

            return message;
        }

        IDisplayController GetDisplayController(MessageQueueItem item)
        {
            if(GetDisplay != null)
            {
                return GetDisplay(item);
            }
            else
            {
                return new TimedDisplay(item, 1.0f);
            }
        }

        ITransitionController GetTransitionController(MessageQueueItem item1, MessageQueueItem item2)
        {
            if(GetTransition != null)
            {
                return GetTransition(item1, item2);
            }
            else
            {
                return new NullTransition(item1, item2);
            }
        }

        bool RaiseMessageQueueExhauseted()
        {
            var handler = ExhaustedQueue;
            if(handler != null)
            {
                return handler(Queue, m_currentItem);
            }
            else
            {
                return false;
            }
        }

        MessageQueueItem m_currentItem;
        ITransitionController m_currentTransition;
    }
}
