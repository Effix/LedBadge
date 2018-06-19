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
        public static int Version { get { return 3; } }

        public BadgeConnection(string port, int baud, IBadgeResponseDispatcher dispatcher, int retryInterval = 500)
        {
            m_dispatcher = dispatcher;

            Port = port;
            Baud = baud;
            Stream = new SerialPort(Port, Baud, Parity.None, 8, StopBits.One);
            Stream.DataReceived += DataReceived;
            Stream.Open();
            
            // 
            for(byte i = 2; i != 0; ++i)
            {
                m_availableIds.Enqueue(i);
            }
            m_nextPacketID = 1;

            //
            if(System.Diagnostics.Debugger.IsAttached)
            {
                retryInterval = 5000;
            }
            m_timer = new System.Threading.Timer(TimeoutEvent, null, retryInterval, retryInterval);
            m_timerInterval = retryInterval;

            //
            Stream.Write(Enumerable.Repeat<byte>(0xFF, 256).ToArray(), 0, 256);
            MemoryStream m = new MemoryStream();
            BadgeCommands.CreateQuerySetting(m, SettingValue.Caps);
            Send(m, true, true);
        }

        void IDisposable.Dispose()
        {
            Close();
        }

        public void Close()
        {
            if(Stream != null)
            {
                Stream.Close();
                Stream = null;
            }

            if(m_timer != null)
            {
                m_timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                m_timer.Dispose();
                m_timer = null;
            }
        }

        public void Send(MemoryStream commands, bool ensureDelivery, bool flush)
        {
            Send(commands.GetBuffer(), 0, (byte)commands.Length, ensureDelivery, flush, 1, true);
        }

        public void Send(byte[] buffer, int offset, byte length, bool ensureDelivery, bool flush)
        {
            Send(buffer, offset, length, ensureDelivery, flush, 1, true);
        }

        void PumpResend()
        {
            PendingPacket[] resend;
            lock(m_resendPackets)
            {
                resend = m_resendPackets.ToArray();
                m_resendPackets.Clear();
            }
            foreach(var packet in resend)
            {
                lock(m_lockObj)
                {
                    lock(m_pendingPackets)
                    {
                        m_pendingPackets.Add(new PendingPacket
                        {
                            TimeStamp = Environment.TickCount,
                            Attempt = packet.Attempt + 1,
                            Cookie = m_nextPacketID,
                            Packet = packet.Packet
                        });
                    }
                    SendPacket(packet.Packet, 0, (byte)packet.Packet.Length, true, false);
                }
            }
        }

        void Send(byte[] buffer, int offset, byte length, bool ensureDelivery, bool flush, int attempt, bool pumpResend)
        {
            if(pumpResend)
            {
                PumpResend();
            }

            lock(m_lockObj)
            {
                //
                if(ensureDelivery)
                {
                    byte[] bufferCopy = new byte[length];
                    Array.Copy(buffer, offset, bufferCopy, 0, length);
                    lock(m_pendingPackets)
                    {
                        m_pendingPackets.Add(new PendingPacket
                        {
                            TimeStamp = Environment.TickCount,
                            Attempt = attempt,
                            Cookie = m_nextPacketID,
                            Packet = bufferCopy
                        });
                    }
                }

                SendPacket(buffer, offset, length, ensureDelivery, flush);
            }
        }

        void SendPacket(byte[] buffer, int offset, byte length, bool ensureDelivery, bool flush)
        {
            lock(m_lockObj)
            {
                //
                ushort dataCrc = 0xFFFF;
                for(int i = 0; i < length; ++i)
                {
                    dataCrc = crc_ccitt_update(dataCrc, buffer[i + offset]);
                }

                //
                m_tempHeader[5] =
                m_tempHeader[0] = 0xA5;
                m_tempHeader[1] = ensureDelivery ? AcquirePacketId() : (byte)0;
                m_tempHeader[2] = length;
                m_tempHeader[3] = (byte)(dataCrc & 0xFF);
                m_tempHeader[4] = (byte)(dataCrc >> 8);
                for(int i = 1; i < 5; ++i)
                {
                    m_tempHeader[5] = crc8_ccitt_update(m_tempHeader[5], m_tempHeader[i]);
                }

                //
                Stream.Write(m_tempHeader, 0, m_tempHeader.Length);
                Stream.Write(buffer, offset, length);
                if(flush)
                {
                    Stream.BaseStream.Flush();
                }
            }
        }

        void TimeoutEvent(object state)
        {
            while(true)
            {
                PendingPacket packet = ExpirePendingPacket(Environment.TickCount);
                if(packet.Packet != null)
                {
                    if(packet.Attempt >= m_retryMax)
                    {
                        m_dispatcher.NotifySendFailure(this, packet.Packet);
                    }
                    else
                    {
                        lock(m_resendPackets)
                        {
                            m_resendPackets.Add(packet);
                        }
                    }
                }
                else
                {
                    break;
                }
            }

            PumpResend();
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

                    // handle a couple of special responses before forwarding them along
                    if(code == ResponseCodes.Ack)
                    {
                        bool fromSerialPacket = ((fullResponse[0] & 0x08) == 0);
                        if(fromSerialPacket)
                        {
                            // reliable packet success!
                            RetirePendingPacket(fullResponse[1]);
                        }
                    }
                    else if(code == ResponseCodes.Error)
                    {
                        ErrorCodes error = (ErrorCodes)(fullResponse[0] & 0xF);
                        if(error == ErrorCodes.CorruptPacketData || error == ErrorCodes.ReceiveBufferOverrun)
                        {
                            // reliable packet failure!
                            PendingPacket packet = RetirePendingPacket(fullResponse[1]);
                            if(packet.Packet != null)
                            {
                                lock(m_resendPackets)
                                {
                                    m_resendPackets.Add(packet);
                                }
                            }
                        }
                    }
                    else if(code == ResponseCodes.Setting)
                    {
                        SettingValue valueType = (SettingValue)(fullResponse[0] & 0xF);
                        if(valueType == SettingValue.Caps)
                        {
                            byte version, width, height, bitDepth;
                            SupportedFeatures features;
                            BadgeResponses.DecodeCapsSetting(fullResponse, 0, out version, out width, out height, out bitDepth, out features);
                            Device = new BadgeCaps(version, width, height, bitDepth, features, Baud);
                        }
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

        PendingPacket RetirePendingPacket(byte id)
        {
            bool found = false;
            PendingPacket packet = new PendingPacket();
            lock(m_pendingPackets)
            {
                for(int pInd = 0, count = m_pendingPackets.Count; pInd < count; ++pInd)
                {
                    if(m_pendingPackets[pInd].Cookie == id)
                    {
                        packet = m_pendingPackets[pInd];
                        packet.Cookie = 0;
                        m_pendingPackets.RemoveAt(pInd);
                        found = true;
                        break;
                    }
                }
            }
            if(found)
            {
                lock(m_availableIds)
                {
                    m_availableIds.Enqueue(id);
                }
            }
            return packet;
        }

        PendingPacket ExpirePendingPacket(int time)
        {
            byte id = 0;
            PendingPacket packet = new PendingPacket();
            lock(m_pendingPackets)
            {
                if(m_pendingPackets.Count > 0)
                {
                    if((time - m_pendingPackets[0].TimeStamp) > m_timerInterval)
                    {
                        packet = m_pendingPackets[0];
                        id = packet.Cookie;
                        packet.Cookie = 0;
                        m_pendingPackets.RemoveAt(0);
                    }
                }
            }
            if(id != 0)
            {
                lock(m_availableIds)
                {
                    m_availableIds.Enqueue(id);
                }
            }
            return packet;
        }

        byte AcquirePacketId()
        {
            lock(m_availableIds)
            {
                byte id = m_nextPacketID;
                m_nextPacketID = m_availableIds.Dequeue();
                return id;
            }
        }

        byte crc8_ccitt_update(byte inCrc, byte inData)
        {
            byte data = (byte)(inCrc ^ inData);

            for(int i = 0; i < 8; i++)
            {
                if((data & 0x80) != 0)
                {
                    data <<= 1;
                    data ^= 0x07;
                }
                else
                {
                    data <<= 1;
                }
            }
            return data;
        }

        ushort crc_ccitt_update(ushort crc, byte data)
        {
            data ^= (byte)(crc & 0xFF);
            data ^= (byte)(data << 4);

            return (ushort)(((data << 8) | (byte)(crc >> 8)) ^ (data >> 4) ^ (data << 3));
        }

        struct PendingPacket
        {
            public int Attempt;
            public int TimeStamp;
            public byte Cookie;
            public byte[] Packet;
        }

        public string Port { get; private set; }
        public int Baud { get; private set; }
        public SerialPort Stream { get; private set; }
        public BadgeCaps Device { get; private set; }

        int m_timeSinceLastSend = Environment.TickCount;
        object m_lockObj = new object();
        List<PendingPacket> m_pendingPackets = new List<PendingPacket>();
        List<PendingPacket> m_resendPackets = new List<PendingPacket>();
        Queue<byte> m_availableIds = new Queue<byte>();
        byte m_nextPacketID;
        System.Threading.Timer m_timer;
        int m_timerInterval;
        int m_retryMax = 5;
        IBadgeResponseDispatcher m_dispatcher;
        byte[] m_inputBuffer = new byte[8192];
        byte[] m_tempHeader = new byte[6];
        int m_inputBufferLength;
    }
}
