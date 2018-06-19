using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LedBadgeLib
{
    /// <summary>
    /// Methods to construct badge commands into a stream of data.
    /// </summary>
    public static class BadgeCommands
    {
        public static void CreatePing(Stream stream, bool echo, byte cookie)
        {
            stream.WriteByte((byte)(((byte)CommandCodes.Ping << 4) | ((echo ? 1 : 0) << 3)));
            stream.WriteByte(cookie);
        }

        public static void CreateQuerySetting(Stream stream, SettingValue setting)
        {
            stream.WriteByte((byte)(((byte)CommandCodes.QuerySetting << 4) | ((byte)setting & 0xF)));
            stream.WriteByte(0);
        }

        public static void CreateUpdateBrightnessSetting(Stream stream, byte brightness)
        {
            stream.WriteByte((byte)(((byte)CommandCodes.UpdateSetting << 4) | ((byte)SettingValue.Brightness)));
            stream.WriteByte(brightness);
        }

        public static void CreateUpdateHoldTimingsSetting(Stream stream, byte a, byte b, byte c)
        {
            stream.WriteByte((byte)(((byte)CommandCodes.UpdateSetting << 4) | ((byte)SettingValue.HoldTimings)));
            stream.WriteByte((byte)((a << 4) | (b & 0xF)));
            stream.WriteByte((byte)(c << 4));
        }

        public static void CreateUpdateIdleTimeoutSetting(Stream stream, byte timeout, bool enableFade, EndofFadeAction endOfFade)
        {
            stream.WriteByte((byte)(((byte)CommandCodes.UpdateSetting << 4) | ((byte)SettingValue.IdleTimeout)));
            stream.WriteByte(timeout);
            stream.WriteByte((byte)((enableFade ? 0x80 : 0) | (((byte)endOfFade & 0x3) << 5)));
        }

        public static void CreateUpdateFadeValueSetting(Stream stream, byte fadeValue, FadingAction action)
        {
            stream.WriteByte((byte)(((byte)CommandCodes.UpdateSetting << 4) | ((byte)SettingValue.FadeValue)));
            stream.WriteByte(fadeValue);
            stream.WriteByte((byte)((byte)action << 6));
        }

        public static void CreateUpdateAnimBookmarkPosSetting(Stream stream, short position)
        {
            stream.WriteByte((byte)(((byte)CommandCodes.UpdateSetting << 4) | ((byte)SettingValue.AnimBookmarkPos)));
            stream.WriteByte((byte)(position >> 8));
            stream.WriteByte((byte)(position & 0xFF));
        }

        public static void CreateUpdateAnimReadPosSetting(Stream stream, short position)
        {
            stream.WriteByte((byte)(((byte)CommandCodes.UpdateSetting << 4) | ((byte)SettingValue.AnimReadPos)));
            stream.WriteByte((byte)(position >> 8));
            stream.WriteByte((byte)(position & 0xFF));
        }

        public static void CreateUpdateAnimPlayStateSetting(Stream stream, AnimState playState)
        {
            stream.WriteByte((byte)(((byte)CommandCodes.UpdateSetting << 4) | ((byte)SettingValue.AnimPlayState)));
            stream.WriteByte((byte)((byte)playState & 0x3));
        }

        public static void CreateSwap(Stream stream, bool bookmark, byte holdFrames)
        {
            stream.WriteByte((byte)(((byte)CommandCodes.Swap << 4) | (bookmark ? 0x08 : 0)));
            stream.WriteByte(holdFrames);
        }

        public static void CreateReadRect(Stream stream, Target targetBuffer, PixelFormat format, byte x, byte y, byte width, byte height)
        {
            stream.WriteByte((byte)(((byte)CommandCodes.ReadRect << 4) | (((byte)targetBuffer & 0x3) << 2) | ((byte)format & 0x3)));
            stream.WriteByte((byte)((x << 4) | (y & 0xF)));
            stream.WriteByte((byte)((width << 4) | (height & 0xF)));
        }

        public static void CreateWriteRect(Stream stream, Target targetBuffer, PixelFormat format, byte x, byte y, byte width, byte height, out int bufferSize)
        {
            stream.WriteByte((byte)(((byte)CommandCodes.WriteRect << 4) | (((byte)targetBuffer & 0x3) << 2) | ((byte)format & 0x3)));
            stream.WriteByte((byte)((x << 4) | (y & 0xF)));
            stream.WriteByte((byte)((width << 4) | (height & 0xF)));
            bufferSize = width * height * ((int)format + 1);
        }

        public static void CreateCopyRect(Stream stream, Target sourceBuffer, Target targetBuffer, byte srcX, byte srcY, byte dstX, byte dstY, byte width, byte height)
        {
            stream.WriteByte((byte)(((byte)CommandCodes.CopyRect << 4) | (((byte)sourceBuffer & 0x3) << 2) | ((byte)targetBuffer & 0x3)));
            stream.WriteByte((byte)((srcX << 4) | (srcY & 0xF)));
            stream.WriteByte((byte)((dstX << 4) | (dstY & 0xF)));
            stream.WriteByte((byte)((width << 4) | (height & 0xF)));
        }

        public static void CreateFillRect(Stream stream, Target targetBuffer, byte x, byte y, byte width, byte height, Pix2x8 value)
        {
            stream.WriteByte((byte)(((byte)CommandCodes.FillRect << 4) | (((byte)targetBuffer & 0x3) << 2)));
            stream.WriteByte((byte)((x << 4) | (y & 0xF)));
            stream.WriteByte((byte)((width << 4) | (height & 0xF)));
            stream.WriteByte((byte)(value.Value >> 8));
            stream.WriteByte((byte)(value.Value & 0xFF));
        }

        public static void CreateReadMemory(Stream stream, short address, int numDWords)
        {
            if(numDWords < 1) { numDWords = 1; }
            if(numDWords > 16) { numDWords = 16; }

            stream.WriteByte((byte)(((byte)CommandCodes.ReadMemory << 4) | (numDWords - 1)));
            stream.WriteByte((byte)(address >> 8));
            stream.WriteByte((byte)(address & 0xFF));
        }

        public static void CreateWriteMemory(Stream stream, short address, int numDWords, out int bufferSize)
        {
            if(numDWords < 1) { numDWords = 1; }
            if(numDWords > 16) { numDWords = 16; }
            bufferSize = numDWords * 4;

            stream.WriteByte((byte)(((byte)CommandCodes.WriteMemory << 4) | (numDWords - 1)));
            stream.WriteByte((byte)(address >> 8));
            stream.WriteByte((byte)(address & 0xFF));
        }

        public static void CreateWriteMemory(Stream stream, short address, byte[] data)
        {
            int bufferSize;
            CreateWriteMemory(stream, address, data.Length / 4, out bufferSize);
            for(int i = 0; i < bufferSize; ++i)
            {
                stream.WriteByte(data[i++]);
            }
        }

        public static void CreatePlayFromBookmark(Stream stream, AnimState playState, short? bookmark = null)
        {
            stream.WriteByte((byte)(((byte)CommandCodes.PlayFromBookmark << 4) | (bookmark.HasValue ? 0x08 : 0) | ((byte)playState & 0x3)));
            stream.WriteByte((byte)(bookmark.HasValue ? bookmark.Value >> 8 : 0));
            stream.WriteByte((byte)(bookmark.HasValue ? bookmark.Value & 0xFF : 0));
        }

        public static CommandCodes GetCode(byte b)
        {
            return (CommandCodes)(b >> 4);
        }

        public static int GetMinCommandLength(CommandCodes command)
        {
            switch(command)
            {
                case CommandCodes.Ping:             return 2;
                case CommandCodes.QuerySetting:     return 2;
                case CommandCodes.UpdateSetting:    return 2;
                case CommandCodes.Swap:             return 2;
                case CommandCodes.ReadRect:         return 3;
                case CommandCodes.WriteRect:        return 3;
                case CommandCodes.CopyRect:         return 4;
                case CommandCodes.FillRect:         return 5;
                case CommandCodes.ReadMemory:       return 3;
                case CommandCodes.WriteMemory:      return 3;
                case CommandCodes.PlayFromBookmark: return 3;
            }
            throw new NotImplementedException("Unimplemented CommandCode length! (" + command + ")");
        }

        public static int GetSettingUpdateLength(SettingValue setting)
        {
            switch(setting)
            {
                case SettingValue.Brightness:       return 2;
                case SettingValue.HoldTimings:      return 3;
                case SettingValue.IdleTimeout:      return 3;
                case SettingValue.FadeValue:        return 3;
                case SettingValue.AnimBookmarkPos:  return 3;
                case SettingValue.AnimReadPos:      return 3;
                case SettingValue.AnimPlayState:    return 2;
            }
            throw new NotImplementedException("Unimplemented SettingValue length! (" + setting + ")");
        }

        public static int GetFullCommandLength(CommandCodes command, byte[] buffer, int offset)
        {
            switch(command)
            {
                case CommandCodes.UpdateSetting:
                {
                    SettingValue setting = (SettingValue)(buffer[offset] & 0xF);
                    return GetSettingUpdateLength(setting);
                }
                case CommandCodes.WriteRect:
                {
                    Target targetBuffer;
                    PixelFormat format;
                    byte x, y;
                    byte width, height;
                    byte bufferLength;
                    int headerLen = BadgeCommands.DecodeWriteRect(buffer, offset, out targetBuffer, out format, out x, out y, out width, out height, out bufferLength);
                    return headerLen + bufferLength;
                }
                case CommandCodes.WriteMemory:
                {
                    byte numDWords;
                    short address;
                    byte bufferLength;
                    int headerLen = BadgeCommands.DecodeWriteMemory(buffer, offset, out address, out numDWords, out bufferLength);
                    return headerLen + bufferLength;
                }
                default: return GetMinCommandLength(command);
            }
        }

        public static int DecodePing(byte[] buffer, int offset, out bool echo, out byte cookie)
        {
            System.Diagnostics.Debug.Assert((CommandCodes)(buffer[offset] >> 4) == CommandCodes.Ping);

            echo = (buffer[offset] & 0x8) != 0;
            cookie = buffer[offset + 1];
            return 2;
        }

        public static int DecodeQuerySetting(byte[] buffer, int offset, out SettingValue setting)
        {
            System.Diagnostics.Debug.Assert((CommandCodes)(buffer[offset] >> 4) == CommandCodes.QuerySetting);

            setting = (SettingValue)(buffer[offset] & 0xF);
            return 2;
        }

        public static int DecodeUpdateBrightnessSetting(byte[] buffer, int offset, out byte brightness)
        {
            System.Diagnostics.Debug.Assert((CommandCodes)(buffer[offset] >> 4) == CommandCodes.UpdateSetting);
            System.Diagnostics.Debug.Assert((SettingValue)(buffer[offset] & 0xF) == SettingValue.Brightness);

            brightness = buffer[offset];
            return 2;
        }

        public static int DecodeUpdateHoldTimingsSetting(byte[] buffer, int offset, out byte a, out byte b, out byte c)
        {
            System.Diagnostics.Debug.Assert((CommandCodes)(buffer[offset] >> 4) == CommandCodes.UpdateSetting);
            System.Diagnostics.Debug.Assert((SettingValue)(buffer[offset] & 0xF) == SettingValue.HoldTimings);

            a = (byte)(buffer[offset + 1] >> 4);
            b = (byte)(buffer[offset + 1] & 0xF);
            c = (byte)(buffer[offset + 2] >> 4);
            return 3;
        }

        public static int DecodeUpdateIdleTimeoutSetting(byte[] buffer, int offset, out byte timeout, out bool enableFade, out EndofFadeAction endOfFade)
        {
            System.Diagnostics.Debug.Assert((CommandCodes)(buffer[offset] >> 4) == CommandCodes.UpdateSetting);
            System.Diagnostics.Debug.Assert((SettingValue)(buffer[offset] & 0xF) == SettingValue.IdleTimeout);

            timeout = buffer[offset + 1];
            enableFade = (buffer[offset + 2] & 0x80) != 0;
            endOfFade = (EndofFadeAction)((buffer[offset + 2] >> 5) & 0x3);
            return 3;
        }

        public static int DecodeUpdateFadeValueSetting(byte[] buffer, int offset, out byte fadeValue, out FadingAction action)
        {
            System.Diagnostics.Debug.Assert((CommandCodes)(buffer[offset] >> 4) == CommandCodes.UpdateSetting);
            System.Diagnostics.Debug.Assert((SettingValue)(buffer[offset] & 0xF) == SettingValue.FadeValue);

            fadeValue = buffer[offset + 1];
            action = (FadingAction)((buffer[offset + 2] >> 6) & 0x3);
            return 3;
        }

        public static int DecodeUpdateAnimBookmarkPosSetting(byte[] buffer, int offset, out short position)
        {
            System.Diagnostics.Debug.Assert((CommandCodes)(buffer[offset] >> 4) == CommandCodes.UpdateSetting);
            System.Diagnostics.Debug.Assert((SettingValue)(buffer[offset] & 0xF) == SettingValue.AnimBookmarkPos);

            position = (short)((buffer[offset + 1] << 8) | buffer[offset + 2]);
            return 3;
        }

        public static int DecodeUpdateAnimReadPosSetting(byte[] buffer, int offset, out short position)
        {
            System.Diagnostics.Debug.Assert((CommandCodes)(buffer[offset] >> 4) == CommandCodes.UpdateSetting);
            System.Diagnostics.Debug.Assert((SettingValue)(buffer[offset] & 0xF) == SettingValue.AnimReadPos);

            position = (short)((buffer[offset + 1] << 8) | buffer[offset + 2]);
            return 3;
        }

        public static int DecodeUpdateAnimPlayStateSetting(byte[] buffer, int offset, out AnimState playState)
        {
            System.Diagnostics.Debug.Assert((CommandCodes)(buffer[offset] >> 4) == CommandCodes.UpdateSetting);
            System.Diagnostics.Debug.Assert((SettingValue)(buffer[offset] & 0xF) == SettingValue.AnimPlayState);

            playState = (AnimState)(buffer[offset + 1] & 0x3);
            return 2;
        }

        public static int DecodeSwap(byte[] buffer, int offset, out bool bookmark, out byte holdFrames)
        {
            System.Diagnostics.Debug.Assert((CommandCodes)(buffer[offset] >> 4) == CommandCodes.Swap);

            bookmark = (buffer[offset] & 0x8) != 0;
            holdFrames = buffer[offset + 1];
            return 2;
        }

        public static int DecodeReadRect(byte[] buffer, int offset, out Target targetBuffer, out PixelFormat format, out byte x, out byte y, out byte width, out byte height)
        {
            System.Diagnostics.Debug.Assert((CommandCodes)(buffer[offset] >> 4) == CommandCodes.ReadRect);

            targetBuffer = (Target)((buffer[offset] >> 2) & 0x3);
            format = (PixelFormat)(buffer[offset] & 0x3);
            x = (byte)(buffer[offset + 1] >> 4);
            y = (byte)(buffer[offset + 1] & 0xF);
            width = (byte)(buffer[offset + 2] >> 4);
            height = (byte)(buffer[offset + 2] & 0xF);
            return 3;
        }

        public static int DecodeWriteRect(byte[] buffer, int offset, out Target targetBuffer, out PixelFormat format, out byte x, out byte y, out byte width, out byte height, out byte bufferLength)
        {
            System.Diagnostics.Debug.Assert((CommandCodes)(buffer[offset] >> 4) == CommandCodes.WriteRect);

            targetBuffer = (Target)((buffer[offset] >> 2) & 0x3);
            format = (PixelFormat)(buffer[offset] & 0x3);
            x = (byte)(buffer[offset + 1] >> 4);
            y = (byte)(buffer[offset + 1] & 0xF);
            width = (byte)(buffer[offset + 2] >> 4);
            height = (byte)(buffer[offset + 2] & 0xF);
            bufferLength = (byte)(width * height * ((int)format + 1));
            return 3;
        }

        public static int DecodeCopyRect(byte[] buffer, int offset, out Target sourceBuffer, out Target targetBuffer, out byte srcX, out byte srcY, out byte dstX, out byte dstY, out byte width, out byte height)
        {
            System.Diagnostics.Debug.Assert((CommandCodes)(buffer[offset] >> 4) == CommandCodes.CopyRect);

            sourceBuffer = (Target)((buffer[offset] >> 2) & 0x3);
            targetBuffer = (Target)(buffer[offset] & 0x3);
            srcX = (byte)(buffer[offset + 1] >> 4);
            srcY = (byte)(buffer[offset + 1] & 0xF);
            dstX = (byte)(buffer[offset + 2] >> 4);
            dstY = (byte)(buffer[offset + 2] & 0xF);
            width = (byte)(buffer[offset + 3] >> 4);
            height = (byte)(buffer[offset + 3] & 0xF);
            return 4;
        }

        public static int DecodeFillRect(byte[] buffer, int offset, out Target targetBuffer, out byte x, out byte y, out byte width, out byte height, out Pix2x8 value)
        {
            System.Diagnostics.Debug.Assert((CommandCodes)(buffer[offset] >> 4) == CommandCodes.FillRect);

            targetBuffer = (Target)((buffer[offset] >> 2) & 0x3);
            x = (byte)(buffer[offset + 1] >> 4);
            y = (byte)(buffer[offset + 1] & 0xF);
            width = (byte)(buffer[offset + 2] >> 4);
            height = (byte)(buffer[offset + 2] & 0xF);
            value = new Pix2x8((ushort)((buffer[offset + 3] << 8) | buffer[offset + 4]));
            return 5;
        }

        public static int DecodeReadMemory(byte[] buffer, int offset, out short address, out byte numDWords)
        {
            System.Diagnostics.Debug.Assert((CommandCodes)(buffer[offset] >> 4) == CommandCodes.ReadMemory);

            numDWords = (byte)((buffer[offset] & 0xF) + 1);
            address = (short)((buffer[offset + 1] << 8) | buffer[offset + 2]);
            return 3;
        }

        public static int DecodeWriteMemory(byte[] buffer, int offset, out short address, out byte numDWords, out byte bufferLength)
        {
            System.Diagnostics.Debug.Assert((CommandCodes)(buffer[offset] >> 4) == CommandCodes.WriteMemory);

            numDWords = (byte)((buffer[offset] & 0xF) + 1);
            address = (short)((buffer[offset + 1] << 8) | buffer[offset + 2]);
            bufferLength = (byte)(numDWords * 4);
            return 3;
        }

        public static int DecodePlayFromBookmark(byte[] buffer, int offset, out AnimState playState, out short? bookmark)
        {
            System.Diagnostics.Debug.Assert((CommandCodes)(buffer[offset] >> 4) == CommandCodes.PlayFromBookmark);

            playState = (AnimState)(buffer[offset] & 0x3);
            bookmark = ((buffer[offset] & 0x08) != 0) ? (short)((buffer[offset + 1] << 8) | buffer[offset + 2]) : (short?)null;
            return 3;
        }
    }
}
