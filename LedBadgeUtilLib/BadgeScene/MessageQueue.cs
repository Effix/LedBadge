using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LedBadgeLib
{
    public enum QueuePriority
    {
        Low,
        Medium,
        High
    }

    public enum QueueLocation
    {
        Back,
        Front
    }

    public class MessageQueue
    {
        public MessageQueue(BadgeCaps device)
        {
            Device = device;
            SyncRoot = new object();
        }

        public MessageQueueItem Enqueue(MessageQueueItem message, QueuePriority priority = QueuePriority.Medium, QueueLocation location = QueueLocation.Back)
        {
            lock(SyncRoot)
            {
                if(location == QueueLocation.Back)
                {
                    m_queue[(int)priority].Add(message);
                }
                else
                {
                    m_queue[(int)priority].Insert(0, message);
                }
            }

            return message;
        }

        public MessageQueueItem Dequeue()
        {
            MessageQueueItem message = null;

            lock(SyncRoot)
            {
                for(int i = m_queue.Length - 1; i >= 0; --i)
                {
                    if(m_queue[i].Count > 0)
                    {
                        message = m_queue[i][0];
                        m_queue[i].RemoveAt(0);
                    }
                }
            }

            return message;
        }

        // poor man's priority queue
        List<MessageQueueItem>[] m_queue = new List<MessageQueueItem>[3] 
        { 
            new List<MessageQueueItem>(), 
            new List<MessageQueueItem>(), 
            new List<MessageQueueItem>() 
        };

        public BadgeCaps Device { get; private set; }
        public object SyncRoot { get; private set; }
    }
}
