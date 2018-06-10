#ifndef COMMANDS_H_
#define COMMANDS_H_

#include "Eeprom.h"

#define VERSION 3

struct SerialCommands
{
	enum Enum
	{
		Ping,				// Asking the badge to return the given cookie (or a No-op if cookie is zero)
		QuerySetting,		// 
		UpdateSetting,		// 
        Swap,				// Wait for a vblank and swap the front/back render target
		ReadRect,			// 
		WriteRect,			// 
		CopyRect,			// Copy a block of pixels from a location in a buffer to another
		FillRect,			// 
		ReadMemory,			// 
		WriteMemory,		// 
		AnimControl,		// 
		Fade,				// 
		
		Count
	};
};

struct AnimCommands
{
	enum Enum
	{
		Ping,				// 
		UpdateSetting,		// 
		Swap,				// 
		WriteRect,			// 
		CopyRect,			// 
		FillRect,			// 
		AnimControl,		// 
		Fade,				// 

		Count
	};
};

struct Settings
{
	enum Enum
	{
		Brightness,			// 
		HoldTimings,		// 
		IdleTimeout,		// 
		FadeValue,			// 
		AnimBookmarkPos,	// 
		AnimPlayState,		// 
		ButtonState,		// 
		BufferFullness,		// 
		Caps,				// 
		
		Count
	};
};

struct ResponseCodes
{
	enum Enum
	{
        Ack,				// Ping/Ack response with cookie
		Setting,			// 
		Pixels,				// 
		Memory,				// 
		Error,				// 
		
		Count
	};
};

struct SupportedFeatures
{
	enum Enum
	{
		HardwareBrightness = 0x01,	// Supports fine grained PWM brightness
	};
};

struct ErrorCodes
{
	enum Enum
	{
		Ok,
		CorruptPacketHeader,
		CorruptPacketData,
		ReceiveBufferOverrun,
		EepromWriteOutOfBounds,
		BadSerialCommand,
		BadAnimCommand,

		Count
	};
};

struct BufferTarget
{
	enum Enum
    {
        BackBuffer,
        FrontBuffer,
		
		Count
    };
};

struct RomTarget
{
	enum Enum
	{
		Internal,
		External,
		
		Count,

		TypeOffset = 14,
		TypeMask = 0x3 << TypeOffset,
		AddressMask = (1 << TypeOffset) - 1,

		TypeInternal = Internal << TypeOffset,
		TypeExternal = External << TypeOffset,
		
		InternalMask = EepromInternalSize - 1,
		ExternalMask = EepromExternalSize - 1,
	};
};

struct CommandState
{
	unsigned int AnimReadPosition;		// 
	unsigned int AnimBookmark;			// 
	unsigned int AnimFramesToHold;		//
	bool AnimPlaying;					// 
	unsigned char LastCookie;			//
};

extern CommandState g_CommandReg;

void InitAnim();

void DispatchSerialCommand();

void DispatchAnimCommand();

#endif /* COMMANDS_H_ */