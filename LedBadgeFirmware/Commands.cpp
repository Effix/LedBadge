#include "Commands.h"

#include "Serial.h"
#include "Eeprom.h"

// Animation state machine values
AnimState g_AnimReg = {};

typedef unsigned char (*FetchByte)(bool moreBytes);
typedef void (*CommandHandler)(unsigned char header, FetchByte fetch);

void PingCommandHandler(unsigned char header, FetchByte fetch)
{
	unsigned char cookie = fetch(false);
	if(cookie)
	{
		WriteSerialData(ResponseCodes::Ack << 4);
		WriteSerialData(cookie);
	}
}

void QuerySettingCommandHandler(unsigned char header, FetchByte fetch)
{

}

void UpdateSettingCommandHandler(unsigned char header, FetchByte fetch)
{

}

void SwapCommandHandler(unsigned char header, FetchByte fetch)
{

}

void ReadRectCommandHandler(unsigned char header, FetchByte fetch)
{

}

void WriteRectCommandHandler(unsigned char header, FetchByte fetch)
{

}

void CopyRectCommandHandler(unsigned char header, FetchByte fetch)
{

}

void FillRectCommandHandler(unsigned char header, FetchByte fetch)
{

}

void ReadMemoryCommandHandler(unsigned char header, FetchByte fetch)
{

}

void WriteMemoryCommandHandler(unsigned char header, FetchByte fetch)
{

}

void AnimControlCommandHandler(unsigned char header, FetchByte fetch)
{

}

void FadeCommandHandler(unsigned char header, FetchByte fetch)
{

}

unsigned char FetchSerial(bool moreBytes)
{
	return ReadSerialData();
}

unsigned char FetchInternalEEPROM(bool moreBytes)
{
	int readAddr = g_AnimReg.ReadPosition;
	g_AnimReg.ReadPosition = ((g_AnimReg.ReadPosition + 1) & (EepromInternalSize - 1)) | RomTarget::TypeInternal;
	return ReadInternalEEPROM(readAddr);
}

unsigned char FetchExternalEEPROM(bool moreBytes)
{
	g_AnimReg.ReadPosition = ((g_AnimReg.ReadPosition + 1) & (EepromExternalSize - 1)) | RomTarget::TypeExternal;
	return ReadNextByteFromExternalEEPROM(moreBytes);
}

// Helper for re-syncing after an invalid command is processed
// Similar to the buffer overflow re-sync, but this can be called from outside of an interrupt handler
static void BadCommandPanic()
{
	// notify of panic state
	WriteSerialData((ResponseCodes::Error << 4) | ErrorCodes::BadSerialCommand);
	
	unsigned char count = 0;
	for(;;)
	{
		// wait for a new byte
		if(ReadSerialData() == 0xFF)
		{
			// check for the full sequence of nops
			if(++count == 0)
			{
				// finished!
				break;
			}
		}
		else
		{
			// probably still in the middle of frame data... start over
			count = 0;
		}
	}
	
	// ok, all resynchronized
	WriteSerialData(ResponseCodes::Ack << 4);
}

static void BadAnimPanic()
{
	// notify of panic state
	WriteSerialData((ResponseCodes::Error << 4) | ErrorCodes::BadAnimCommand);

	g_AnimReg.Playing = false;
}

static const CommandHandler s_SerialHandlers[SerialCommands::Count] = 
{
	PingCommandHandler,
	QuerySettingCommandHandler,
	UpdateSettingCommandHandler,
	SwapCommandHandler,
	ReadRectCommandHandler,
	WriteRectCommandHandler,
	CopyRectCommandHandler,
	FillRectCommandHandler,
	ReadMemoryCommandHandler,
	WriteMemoryCommandHandler,
	AnimControlCommandHandler,
	FadeCommandHandler
};

void DispatchSerialCommand()
{
	unsigned char commandHeader = ReadSerialData();
	unsigned char command = (commandHeader >> 4) & 0xF;
	
	if(command < SerialCommands::Count)
	{
		s_SerialHandlers[command](commandHeader, FetchSerial);
	}
	else
	{
		BadCommandPanic();
	}
}

static const CommandHandler s_AnimHandlers[AnimCommands::Count] =
{
	PingCommandHandler,
	UpdateSettingCommandHandler,
	SwapCommandHandler,
	WriteRectCommandHandler,
	CopyRectCommandHandler,
	FillRectCommandHandler,
	AnimControlCommandHandler,
	FadeCommandHandler
};

void DispatchAnimCommand()
{
	bool external = (g_AnimReg.ReadPosition & RomTarget::TypeMask) != RomTarget::TypeInternal;

	if(external)
	{
		BeginReadExternalEEPROM(g_AnimReg.ReadPosition);
		unsigned char commandHeader = FetchExternalEEPROM(true);
		unsigned char command = (commandHeader >> 4) & 0xF;
		
		if(command < SerialCommands::Count)
		{
			s_SerialHandlers[command](commandHeader, FetchExternalEEPROM);
		}
		else
		{
			ReadNextByteFromExternalEEPROM(false); // discard junk byte and close connection, but don't call FetchExternalEEPROM/update the read pointer
			BadAnimPanic();
		}
	}
	else
	{
		unsigned char commandHeader = FetchInternalEEPROM(false);
		unsigned char command = (commandHeader >> 4) & 0xF;
		
		if(command < SerialCommands::Count)
		{
			s_SerialHandlers[command](commandHeader, FetchInternalEEPROM);
		}
		else
		{
			BadAnimPanic();
		}
	}
}

void InitAnim()
{

}
