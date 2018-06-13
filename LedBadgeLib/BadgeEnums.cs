using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LedBadgeLib
{
    /// <summary>
    /// Raw command code values that begin the command packets for the badge.
    /// </summary>
    public enum CommandCodes: byte
    {
        /// <summary>Asks the badge to return the given cookie.</summary>
        Ping,
        /// <summary>Requests information from the device.</summary>
        QuerySetting,
        /// <summary>Modifies a setting on the device.</summary>
        UpdateSetting,
        /// <summary>Swaps the front/back render targets.</summary>
        Swap,
        ReadRect,
        WriteRect,
        CopyRect,
        FillRect,
        ReadMemory,
        WriteMemory,
        PlayFromBookmark
    }

    public enum ResponseCodes: byte
    {
        Ack,
        Setting,
        Pixels,
        Memory,
        Error
    }

    public enum ErrorCodes: byte
    {
        Ok,
        CorruptPacketHeader,
        CorruptPacketData,
        ReceiveBufferOverrun,
        EepromWriteOutOfBounds,
        BadSerialCommand,
        BadAnimCommand
    }

    /// <summary>
    /// Information and configurable settings for a connected badge.
    /// Many of these will persist across power cycles.
    /// </summary>
    public enum SettingValue: byte
    {
        /// <summary>Controls the overall output brightness of the leds.</summary>
        Brightness,
        /// <summary>Controls the gray scale levels by setting the hold levels between the bit-planes.</summary>
        HoldTimings,
        /// <summary>Controls the idle timeout duration and behavior.</summary>
        IdleTimeout,
        FadeValue,
        AnimBookmarkPos,
        AnimReadPos,
        AnimPlayState,
        /// <summary>(ReadOnly) Queries the button state.</summary>
        ButtonState,
        /// <summary>(ReadOnly) Queries the state of the input buffer.</summary>
        BufferFullness,
        /// <summary>(ReadOnly) Queries number physical device capabilities and version info.</summary>
        Caps
    }

    public enum ResponseAckSource: byte
    {
        PacketReceived,
        Ping
    }

    public enum FadingAction: byte
    {
        None,
        In,
        Out
    }

    public enum EndofFadeAction: byte
    {
        None,
        Clear,
        RestartAnim,
        ResumeAnim
    }

    public enum PixelFormat: byte
    {
        OneBit,
        TwoBits
    }

    public struct Pix2x8
    {
        public Pix2x8(ushort value): this() { Value = value; }
        public ushort Value { get; set; }
    }

    /// <summary>
    /// Bitflags for supported features.
    /// </summary>
    [Flags]
    public enum SupportedFeatures: byte
    {
        /// <summary>Supports fine grained PWM brightness.</summary>
        HardwareBrightness
    }

    /// <summary>
    /// Identifiers for the command read and write locations.
    /// </summary>
    public enum Target: byte
    {
        /// <summary>The buffer not being displayed. This can be modified without seeing flicker.</summary>
        BackBuffer,
        /// <summary>The buffer being scanned out to the display.</summary>
        FrontBuffer
    }

    public enum AnimState: byte
    {
        Stopped,
        Playing,
        SingleStepping
    }
}
