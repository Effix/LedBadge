using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.IO;
using System.Collections.Concurrent;

namespace LedBadgeLib
{
    public class BadgeConnection: IDisposable
    {
        public BadgeConnection(string port, IBadgeResponseDispatcher dispatcher)
        {
            m_dispatcher = dispatcher;

            Port = port;
            Baud = 128000;
            Stream = new SerialPort(Port, Baud, Parity.None, 8, StopBits.One);
            Stream.DataReceived += DataReceived;
            Stream.Open();

            SendSyncSequence();
        }

        void IDisposable.Dispose()
        {
            Close();
        }

        void DataReceived(object sender, SerialDataReceivedEventArgs data)
        {
            List<byte[]> responses = new List<byte[]>();

            byte[] buffer = new byte[Stream.BytesToRead + m_inputBufferLength];
            Array.Copy(m_inputBuffer, 0, buffer, 0, m_inputBufferLength);
            Stream.Read(buffer, m_inputBufferLength, buffer.Length - m_inputBufferLength);
            m_inputBufferLength = 0;

            for(int i = 0; i < buffer.Length; )
            {
                ResponseCodes code = BadgeResponses.GetCode(buffer[i]);

                int rem = buffer.Length - i;
                int minSize = BadgeResponses.GetMinResponseLength(code);
                int fullSize = int.MaxValue;
                if(minSize <= rem)
                {
                    fullSize = BadgeResponses.GetFullResponseLength(code, buffer, i);
                }

                if(fullSize <= rem)
                {
                    byte[] fullResponse = new byte[fullSize];
                    Array.Copy(buffer, i, fullResponse, 0, fullSize);
                    responses.Add(fullResponse);
                    i += fullSize;

                    if(code == ResponseCodes.BadCommand || code == ResponseCodes.ReceiveOverflow)
                    {
                        m_needsReSync = true;
                    }
                }
                else
                {
                    // needs more
                    m_inputBufferLength = buffer.Length - i;
                    Array.Copy(buffer, i, m_inputBuffer, 0, m_inputBufferLength);
                    break;
                }
            }

            if(responses.Count > 0 && m_dispatcher != null)
            {
                m_dispatcher.EnqueueResponse(this, responses.ToArray());
            }
        }

        public void Close()
        {
            if(Stream != null)
            {
                Stream.Close();
                Stream = null;
            }
        }

        public void SendSyncSequence()
        {
            Stream.Write(new byte[256], 0, 256);
            Stream.Write(new byte[] { (byte)((byte)CommandCodes.Version << 4) }, 0, 1);
            Stream.BaseStream.Flush();
        }

        public void Send(MemoryStream commands, bool flush)
        {
            if(m_needsReSync)
            {
                SendSyncSequence();
                m_needsReSync = false;
            }

            Stream.Write(commands.GetBuffer(), 0, (int)commands.Length);
            if(flush)
            {
                Stream.BaseStream.Flush();
            }
        }

        public string Port { get; private set; }
        public int Baud { get; private set; }
        public SerialPort Stream { get; private set; }

        IBadgeResponseDispatcher m_dispatcher;
        byte[] m_inputBuffer = new byte[8192];
        int m_inputBufferLength;
        bool m_needsReSync;
    }
}
