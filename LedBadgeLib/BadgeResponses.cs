using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LedBadgeLib
{
    public enum ResponseCodes: byte
    {
        Nop,
        Ack,
        Version,
        Pix,
        PixRect,
        Inputs,
        BadCommand,
        ReceiveOverflow
    }

    public static class BadgeResponses
    {
        public static ResponseCodes GetCode(byte b)
        {
            return (ResponseCodes)(b >> 4);
        }

        public static int GetMinResponseLength(ResponseCodes response)
        {
            switch(response)
            {
                case ResponseCodes.PixRect: return 2;
                default: return 1;
            }
        }

        public static int GetFullResponseLength(ResponseCodes response, byte[] buffer, int offset)
        {
            switch(response)
            {
                case ResponseCodes.PixRect:
                {
                    int width;
                    int height;
                    int rectBufferLength;
                    int headerLen = BadgeResponses.DecodePixRect(buffer, offset, out width, out height, out rectBufferLength);
                    return headerLen + rectBufferLength;
                }
                default: return 1;
            }
        }

        public static int DecodeNop(byte[] buffer, int offset)
        {
            System.Diagnostics.Debug.Assert((ResponseCodes)(buffer[offset] >> 4) == ResponseCodes.Nop);

            return 1;
        }

        public static int DecodeAck(byte[] buffer, int offset, out int cookie)
        {
            System.Diagnostics.Debug.Assert((ResponseCodes)(buffer[offset] >> 4) == ResponseCodes.Ack);

            cookie = buffer[offset] & 0xF;
            return 1;
        }

        public static int DecodeVersion(byte[] buffer, int offset, out int version)
        {
            System.Diagnostics.Debug.Assert((ResponseCodes)(buffer[offset] >> 4) == ResponseCodes.Version);

            version = buffer[offset] & 0xF;
            return 1;
        }

        public static int DecodePix(byte[] buffer, int offset, out int value)
        {
            System.Diagnostics.Debug.Assert((ResponseCodes)(buffer[offset] >> 4) == ResponseCodes.Pix);

            value = buffer[offset] & 0xF;
            return 1;
        }

        public static int DecodePixRect(byte[] buffer, int offset, out int width, out int height, out int bufferLength)
        {
            System.Diagnostics.Debug.Assert((ResponseCodes)(buffer[offset] >> 4) == ResponseCodes.PixRect);

            height = buffer[offset] & 0xF;
            width = buffer[offset + 1];
            bufferLength = (width * height + BadgeCaps.PixelsPerByte - 1) / BadgeCaps.PixelsPerByte;
            return 2;
        }

        public static int DecodeInputs(byte[] buffer, int offset, out bool button0, out bool button1)
        {
            System.Diagnostics.Debug.Assert((ResponseCodes)(buffer[offset] >> 4) == ResponseCodes.Inputs);

            button0 = (buffer[offset] & 0x1) != 0;
            button1 = (buffer[offset] & 0x2) != 0;
            return 1;
        }

        public static int DecodeBadCommand(byte[] buffer, int offset)
        {
            System.Diagnostics.Debug.Assert((ResponseCodes)(buffer[offset] >> 4) == ResponseCodes.BadCommand);

            return 1;
        }

        public static int DecodeReceiveOverflow(byte[] buffer, int offset)
        {
            System.Diagnostics.Debug.Assert((ResponseCodes)(buffer[offset] >> 4) == ResponseCodes.ReceiveOverflow);

            return 1;
        }
    }
}
