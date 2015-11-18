using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LedBadgeLib
{
    public enum QueueState
    {
        None, 
        Queued,
        Displaying,
        Interrupted,
        Removed
    }

    public class MessageQueueItem
    {
        public MessageQueueItem(IBadgeVisual element)
        {
            Element = element;
        }

        public IDisplayController DisplayController { get; set; }
        public QueueState State { get; set; }
        public IBadgeVisual Element { get; private set; }

        public void Remove()
        {
            State = QueueState.Removed;
        }
    }
}
