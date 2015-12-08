using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace LedBadgeLib
{
    public class BadgeFrameEventArgs: EventArgs
    {
        public BadgeFrameEventArgs(BadgeRenderTarget buffer)
        {
            Frame = buffer;
        }

        public BadgeRenderTarget Frame { get; set; }
    }

    public delegate void BadgeFrameEventHandler(object sender, BadgeFrameEventArgs args);

    public class BadgeCommandEventArgs: EventArgs
    {
        public BadgeCommandEventArgs(MemoryStream commands)
        {
            CommandStream = commands;
        }

        public MemoryStream CommandStream { get; set; }
    }

    public delegate void BadgeCommandEvenHandler(object sender, BadgeCommandEventArgs args);

    public class BadgePump: IDisposable
    {
        public BadgePump(IBadgeResponseDispatcher dispatcher)
        {
            Brightness = 255;
            UseFrameBuffer = true;
            FrameRate = 60;
            m_responseDispatcher = dispatcher;

            m_thread = new Thread(ThreadBody);
            m_thread.IsBackground = true;
            m_thread.Start();
        }

        public bool Dither { get; set; }
        public byte Brightness { get; set; }
        public bool Connected { get { return m_connection != null; } }
        public bool Running { get; private set; }
        public bool UseFrameBuffer { get; set; }
        public bool RotateFrame { get; set; }
        public int FrameRate { get; set; }
        public bool FrameSync { get; set; }
        public bool StrictFrameTiming { get; set; }

        public event BadgeFrameEventHandler RenderFrame;
        public event BadgeFrameEventHandler FrameReady;
        public event BadgeCommandEvenHandler GenerateCommands;
        public event BadgeCommandEvenHandler ReadyToSend;

        int m_prevBrightness = -1;
        IBadgeResponseDispatcher m_responseDispatcher;
        BadgeConnection m_connection;
        ConcurrentQueue<MemoryStream> m_pendingCommands = new ConcurrentQueue<MemoryStream>();
        BadgeRenderTarget m_renderTarget = new BadgeRenderTarget();
        ManualResetEvent m_cancel = new ManualResetEvent(false);
        ManualResetEvent m_enable = new ManualResetEvent(false);
        Stopwatch m_timer = new Stopwatch();
        Thread m_thread;

        public void Dispose()
        {
            m_cancel.Set();
            m_thread.Join();
            m_thread = null;
        }

        public void Start()
        {
            Running = true;
            m_enable.Set();
        }

        public void Stop()
        {
            m_enable.Reset();
            Running = false;
        }

        public void Disconnect()
        {
            if(Connected)
            {
                m_connection.Close();
                m_connection = null;
            }
        }

        public void Connect(string port)
        {
            if(!Connected)
            {
                m_prevBrightness = -1;
                m_connection = new BadgeConnection(port, m_responseDispatcher);
            }
            else
            {
                throw new Exception("Already connected");
            }
        }

        public void EnqueueCommandsAsync(MemoryStream commands)
        {
            m_pendingCommands.Enqueue(commands);
        }

        void RunFrame()
        {
            var commands = new MemoryStream();
            if(m_prevBrightness != Brightness)
            {
                m_prevBrightness = Brightness;
                BadgeCommands.SetBrightness(commands, Brightness);
            }

            if(UseFrameBuffer)
            {
                var render = RenderFrame;
                if(render != null)
                {
                    render(this, new BadgeFrameEventArgs(m_renderTarget));
                }

                if(Dither)
                {
                    m_renderTarget.DitherImage();
                }
                m_renderTarget.PackBuffer(RotateFrame);

                var ready = FrameReady;
                if(ready != null)
                {
                    ready(this, new BadgeFrameEventArgs(m_renderTarget));
                }

                BadgeCommands.FillRect(commands, 0, 0, m_renderTarget.Width, m_renderTarget.Height, Target.BackBuffer, m_renderTarget.PackedBuffer);
                BadgeCommands.Swap(commands);
            }
            else
            {
                var getCommands = GenerateCommands;
                if(getCommands != null)
                {
                    getCommands(this, new BadgeCommandEventArgs(commands));
                }
            }

            var readyToSend = ReadyToSend;
            if(readyToSend != null)
            {
                readyToSend(this, new BadgeCommandEventArgs(commands));
            }

            SendFrame(commands);
        }

        void SendFrame(MemoryStream commands)
        {
            if(Connected)
            {
                MemoryStream additionalCommands;
                while(m_pendingCommands.TryDequeue(out additionalCommands))
                {
                    if(additionalCommands.Length > 0)
                    {
                        m_connection.Send(additionalCommands, false);
                    }
                }

                if(commands.Length > 0)
                {
                    m_connection.Send(commands, true);
                }
            }
            else
            {
                MemoryStream additionalCommands;
                while(m_pendingCommands.TryDequeue(out additionalCommands))
                {
                }
            }
        }

        void ThreadBody()
        {
            long freq = Stopwatch.Frequency;
            long sleepPad = (long)((1 * freq) / 1000);

            for(;;)
            {
                m_timer.Restart();

                if(EventWaitHandle.WaitAny(new[] { m_cancel, m_enable }) == 0)
                {
                    break;
                }

                long frameRate = FrameRate;
                long frameTime = freq / frameRate;
                bool strictTiming = StrictFrameTiming;

                RunFrame();

                long elapsedTime = m_timer.ElapsedTicks;
                if(FrameSync)
                {
                    long syncedFrameTime = ((elapsedTime + (frameTime - 1)) / frameTime) * frameTime;
                    frameTime = syncedFrameTime;
                }

                long sleepPadThisFrame = strictTiming ? sleepPad : 0;
                long remTime = frameTime - elapsedTime;
                if(remTime > sleepPadThisFrame)
                {
                    int msToSleep = (int)((remTime - sleepPadThisFrame) * 1000 / freq);
                    Thread.Sleep(msToSleep);
                }

                if(strictTiming)
                {
                    for(;;)
                    {
                        elapsedTime = m_timer.ElapsedTicks;
                        remTime = frameTime - elapsedTime;
                        if(remTime <= 0)
                        {
                            break;
                        }
                    }
                }
            }
        }
    }
}
