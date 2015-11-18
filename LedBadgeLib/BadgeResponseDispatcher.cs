using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;

namespace LedBadgeLib
{
    public class BadgeResponseEventArgs: EventArgs
    {
        public BadgeResponseEventArgs(BadgeConnection badge, byte[] response)
        {
            FromBadge = badge;
            Response = response;
        }

        public BadgeConnection FromBadge { get; private set; }
        public ResponseCodes Code { get { return (ResponseCodes)(Response[0] >> 4); } }
        public byte[] Response { get; private set; }
    }

    public delegate void ResponseEvenHandler(object sender, BadgeResponseEventArgs args);

    public interface IBadgeResponseDispatcher
    {
        void EnqueueResponse(BadgeConnection badge, byte[][] responses);
        
        event ResponseEvenHandler ResponseHandler;
    }

    /// <summary>
    /// Simple forwarding dispatcher. 
    /// There is a risk of data loss from buffer overflow if the handler blocks for too long.
    /// </summary>
    public class BadgeResponsePassthroughDispatcher: IBadgeResponseDispatcher
    {
        public void EnqueueResponse(BadgeConnection badge, byte[][] responses)
        {
            var handler = ResponseHandler;
            if(handler != null)
            {
                foreach(byte[] response in responses)
                {
                    handler(this, new BadgeResponseEventArgs(badge, response));
                }
            }
        }

        public event ResponseEvenHandler ResponseHandler;
    }

    public class BadgeResponseBufferedDispatcher: IBadgeResponseDispatcher, IDisposable
    {
        public BadgeResponseBufferedDispatcher()
        {
            m_dispatcher = new Thread(ThreadBody);
            m_dispatcher.IsBackground = true;
            m_dispatcher.Start();
        }

        public void Dispose()
        {
            m_cancel = true;
            m_hasWork.Release();
            m_dispatcher.Join();
            m_dispatcher = null;
        }

        public void EnqueueResponse(BadgeConnection badge, byte[][] responses)
        {
            m_Responses.Enqueue(Tuple.Create(badge, responses));
            m_hasWork.Release();
        }

        public event ResponseEvenHandler ResponseHandler;

        void ThreadBody()
        {
            while(!m_cancel)
            {
                m_hasWork.Wait();
                
                var handler = ResponseHandler;

                Tuple<BadgeConnection, byte[][]> responses;
                while(m_Responses.TryDequeue(out responses))
                {
                    if(handler != null)
                    {
                        foreach(byte[] response in responses.Item2)
                        {
                            handler(this, new BadgeResponseEventArgs(responses.Item1, response));
                        }
                    }
                }
            }
        }

        bool m_cancel;
        Thread m_dispatcher;
        SemaphoreSlim m_hasWork = new SemaphoreSlim(0);
        ConcurrentQueue<Tuple<BadgeConnection, byte[][]>> m_Responses = new ConcurrentQueue<Tuple<BadgeConnection, byte[][]>>();
    }
}
