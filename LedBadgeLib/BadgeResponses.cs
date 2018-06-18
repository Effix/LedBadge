using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LedBadgeLib
{
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
                case ResponseCodes.Ack:     return 2;
                case ResponseCodes.Setting: return 2;
                case ResponseCodes.Pixels:  return 2;
                case ResponseCodes.Memory:  return 3;
                case ResponseCodes.Error:   return 2;
            }
            //throw new NotImplementedException("Unimplemented ResponseCode length! (" + response + ")");
            return 1;
        }

        public static int GetSettingResponseLength(SettingValue setting)
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
                case SettingValue.ButtonState:      return 2;
                case SettingValue.BufferFullness:   return 2;
                case SettingValue.Caps:             return 5;
            }
            //throw new NotImplementedException("Unimplemented SettingValue length! (" + setting + ")");
            return 1;
        }

        public static int GetFullResponseLength(ResponseCodes response, byte[] buffer, int offset)
        {
            switch(response)
            {
                case ResponseCodes.Setting:
                {
                    SettingValue setting = (SettingValue)(buffer[offset] & 0xF);
                    return GetSettingResponseLength(setting);
                }
                case ResponseCodes.Pixels:
                {
                    PixelFormat format;
                    byte width;
                    byte height;
                    byte bufferLength;
                    int headerLen = BadgeResponses.DecodePixels(buffer, offset, out format, out width, out height, out bufferLength);
                    return headerLen + bufferLength;
                }
                case ResponseCodes.Memory:
                {
                    byte numDWords;
                    short address;
                    byte bufferLength;
                    int headerLen = BadgeResponses.DecodeMemory(buffer, offset, out numDWords, out address, out bufferLength);
                    return headerLen + bufferLength;
                }
                default: return GetMinResponseLength(response);
            }
        }

        public static int DecodeAck(byte[] buffer, int offset, out ResponseAckSource source, out byte cookie)
        {
            System.Diagnostics.Debug.Assert((ResponseCodes)(buffer[offset] >> 4) == ResponseCodes.Ack);

            source = (ResponseAckSource)((buffer[offset] >> 3) & 0x1);
            cookie = buffer[offset + 1];
            return 2;
        }

        public static int DecodeBrightnessSetting(byte[] buffer, int offset, out byte brightness)
        {
            System.Diagnostics.Debug.Assert((ResponseCodes)(buffer[offset] >> 4) == ResponseCodes.Setting);
            System.Diagnostics.Debug.Assert((SettingValue)(buffer[offset] & 0xF) == SettingValue.Brightness);

            brightness = buffer[offset + 1];
            return 2;
        }

        public static int DecodeHoldTimingsSetting(byte[] buffer, int offset, out byte a, out byte b, out byte c)
        {
            System.Diagnostics.Debug.Assert((ResponseCodes)(buffer[offset] >> 4) == ResponseCodes.Setting);
            System.Diagnostics.Debug.Assert((SettingValue)(buffer[offset] & 0xF) == SettingValue.HoldTimings);

            a = (byte)(buffer[offset + 1] >> 4);
            b = (byte)(buffer[offset + 1] & 0xF);
            c = (byte)(buffer[offset + 2] >> 4);
            return 3;
        }

        public static int DecodeIdleTimeoutSetting(byte[] buffer, int offset, out byte timeout, out bool enableFade, out EndofFadeAction endOfFade)
        {
            System.Diagnostics.Debug.Assert((ResponseCodes)(buffer[offset] >> 4) == ResponseCodes.Setting);
            System.Diagnostics.Debug.Assert((SettingValue)(buffer[offset] & 0xF) == SettingValue.IdleTimeout);

            timeout = buffer[offset + 1];
            enableFade = (buffer[offset + 2] & 0x80) != 0;
            endOfFade = (EndofFadeAction)((buffer[offset + 2] >> 5) & 0x3);
            return 3;
        }

        public static int DecodeFadeValueSetting(byte[] buffer, int offset, out byte fadeValue, out FadingAction action)
        {
            System.Diagnostics.Debug.Assert((ResponseCodes)(buffer[offset] >> 4) == ResponseCodes.Setting);
            System.Diagnostics.Debug.Assert((SettingValue)(buffer[offset] & 0xF) == SettingValue.FadeValue);

            fadeValue = buffer[offset + 1];
            action = (FadingAction)((buffer[offset + 2] >> 6) & 0x3);
            return 3;
        }

        public static int DecodeAnimBookmarkPosSetting(byte[] buffer, int offset, out short position)
        {
            System.Diagnostics.Debug.Assert((ResponseCodes)(buffer[offset] >> 4) == ResponseCodes.Setting);
            System.Diagnostics.Debug.Assert((SettingValue)(buffer[offset] & 0xF) == SettingValue.AnimBookmarkPos);

            position = (short)((buffer[offset + 1] << 8) | buffer[offset + 2]);
            return 3;
        }

        public static int DecodeAnimReadPosSetting(byte[] buffer, int offset, out short position)
        {
            System.Diagnostics.Debug.Assert((ResponseCodes)(buffer[offset] >> 4) == ResponseCodes.Setting);
            System.Diagnostics.Debug.Assert((SettingValue)(buffer[offset] & 0xF) == SettingValue.AnimReadPos);

            position = (short)((buffer[offset + 1] << 8) | buffer[offset + 2]);
            return 3;
        }

        public static int DecodeAnimPlayStateSetting(byte[] buffer, int offset, out AnimState animState)
        {
            System.Diagnostics.Debug.Assert((ResponseCodes)(buffer[offset] >> 4) == ResponseCodes.Setting);
            System.Diagnostics.Debug.Assert((SettingValue)(buffer[offset] & 0xF) == SettingValue.AnimPlayState);

            animState = (AnimState)(buffer[offset + 1] & 0x3);
            return 2;
        }

        public static int DecodeButtonStateSetting(byte[] buffer, int offset, out bool button0, out bool button1)
        {
            System.Diagnostics.Debug.Assert((ResponseCodes)(buffer[offset] >> 4) == ResponseCodes.Setting);
            System.Diagnostics.Debug.Assert((SettingValue)(buffer[offset] & 0xF) == SettingValue.ButtonState);

            button0 = (buffer[offset + 1] & 0x1) != 0;
            button1 = (buffer[offset + 1] & 0x2) != 0;
            return 2;
        }

        public static int DecodeBufferFullnessSetting(byte[] buffer, int offset, out byte fullness)
        {
            System.Diagnostics.Debug.Assert((ResponseCodes)(buffer[offset] >> 4) == ResponseCodes.Setting);
            System.Diagnostics.Debug.Assert((SettingValue)(buffer[offset] & 0xF) == SettingValue.BufferFullness);

            fullness = buffer[offset + 1];
            return 2;
        }

        public static int DecodeCapsSetting(byte[] buffer, int offset, out byte version, out byte width, out byte height, out byte bitDepth, out SupportedFeatures capBits)
        {
            System.Diagnostics.Debug.Assert((ResponseCodes)(buffer[offset] >> 4) == ResponseCodes.Setting);
            System.Diagnostics.Debug.Assert((SettingValue)(buffer[offset] & 0xF) == SettingValue.Caps);

            version = buffer[offset + 1];
            width = buffer[offset + 2];
            height = (byte)(buffer[offset + 3] >> 4);
            bitDepth = (byte)(buffer[offset + 3] & 0xF);
            capBits = (SupportedFeatures)buffer[offset + 4];
            return 5;
        }

        public static int DecodePixels(byte[] buffer, int offset, out PixelFormat format, out byte width, out byte height, out byte bufferLength)
        {
            System.Diagnostics.Debug.Assert((ResponseCodes)(buffer[offset] >> 4) == ResponseCodes.Pixels);

            format = (PixelFormat)(buffer[offset] & 0x3);
            width = (byte)(buffer[offset + 1] >> 4);
            height = (byte)(buffer[offset + 1] & 0xF);
            bufferLength = (byte)(width * height * ((int)format + 1));
            return 2;
        }

        public static int DecodeMemory(byte[] buffer, int offset, out byte numDWords, out short address, out byte bufferLength)
        {
            System.Diagnostics.Debug.Assert((ResponseCodes)(buffer[offset] >> 4) == ResponseCodes.Memory);

            numDWords = (byte)((buffer[offset] & 0xF) + 1);
            address = (short)((buffer[offset + 1] << 8) | buffer[offset + 2]);
            bufferLength = (byte)(numDWords * 4);
            return 3;
        }

        public static int DecodeError(byte[] buffer, int offset, out ErrorCodes error, out byte cookie)
        {
            System.Diagnostics.Debug.Assert((ResponseCodes)(buffer[offset] >> 4) == ResponseCodes.Error);

            error = (ErrorCodes)(buffer[offset] & 0xF);
            cookie = buffer[offset + 1];
            return 2;
        }
    }
}
