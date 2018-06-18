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

    public class BadgeSendFailureEventArgs: EventArgs
    {
        public BadgeSendFailureEventArgs(BadgeConnection badge, byte[] packet)
        {
            FromBadge = badge;
            Packet = packet;
        }

        public BadgeConnection FromBadge { get; private set; }
        public byte[] Packet { get; private set; }
    }

    public delegate void SendFailureEventHandler(object sender, BadgeSendFailureEventArgs args);

    public interface IBadgeResponseDispatcher
    {
        void EnqueueResponse(BadgeConnection badge, byte[][] responses);
        void NotifySendFailure(BadgeConnection badge, byte[] packet);

        event ResponseEvenHandler ResponseHandler;
        event SendFailureEventHandler SendFailureHandler;
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

        public void NotifySendFailure(BadgeConnection badge, byte[] packet)
        {
            var handler = SendFailureHandler;
            if(handler != null)
            {
                handler(this, new BadgeSendFailureEventArgs(badge, packet));
            }
        }

        public event ResponseEvenHandler ResponseHandler;
        public event SendFailureEventHandler SendFailureHandler;
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
            m_responses.Enqueue(Tuple.Create(badge, responses));
            m_hasWork.Release();
        }

        public void NotifySendFailure(BadgeConnection badge, byte[] packet)
        {
            m_failures.Enqueue(Tuple.Create(badge, packet));
            m_hasWork.Release();
        }

        public event ResponseEvenHandler ResponseHandler;
        public event SendFailureEventHandler SendFailureHandler;

        void ThreadBody()
        {
            while(!m_cancel)
            {
                m_hasWork.Wait();
                
                {
                    var handler = ResponseHandler;

                    Tuple<BadgeConnection, byte[][]> responses;
                    while(m_responses.TryDequeue(out responses))
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

                {
                    var handler = SendFailureHandler;

                    Tuple<BadgeConnection, byte[]> failure;
                    while(m_failures.TryDequeue(out failure))
                    {
                        if(handler != null)
                        {
                            handler(this, new BadgeSendFailureEventArgs(failure.Item1, failure.Item2));
                        }
                    }
                }
            }
        }

        bool m_cancel;
        Thread m_dispatcher;
        SemaphoreSlim m_hasWork = new SemaphoreSlim(0);
        ConcurrentQueue<Tuple<BadgeConnection, byte[][]>> m_responses = new ConcurrentQueue<Tuple<BadgeConnection, byte[][]>>();
        ConcurrentQueue<Tuple<BadgeConnection, byte[]>> m_failures = new ConcurrentQueue<Tuple<BadgeConnection, byte[]>>();
    }
}
