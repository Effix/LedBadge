
#include "Commands.h"
#include "Display.h"
#include "Buttons.h"
#include "Serial.h"
#include "I2C.h"
#include "Eeprom.h"
#include <util/delay_basic.h>
#include <util/atomic.h>

// Power up init
void Setup()
{
	// ports and io
	ConfigureDisplay();
	ConfigurePushButtons();
	ConfigureUART();
	ConfigureI2C();
	ConfigureExternalEEPROM();
	InitAnim();
	
	sei();
}

int main(void)
{
	Setup();
	
	for(;;)
    {
		if(GetPendingSerialDataSize())
		{
			DispatchSerialCommand();
			ResetIdleTime();
		}
		else if(g_AnimReg.Playing)
		{
			DispatchAnimCommand();
		}
	}


		/*
			case CommandCodes::Version:
			{
				// respond with the version of this firmware
				#if VERSION > 15
				#error Overflow writing version to the response stream
				#endif
				WriteSerialData((ResponseCodes::Version << 4) | VERSION);
				break;
			}
			case CommandCodes::Swap:
			{
				// flip the frame
				SwapBuffers();
				break;
			}
			case CommandCodes::PollInputs:
			{
				// respond with the current button state
				WriteSerialData((ResponseCodes::Inputs << 4) | (CheckButton1() << 1) | CheckButton0());
				break;
			}
			case CommandCodes::SetBrightness:
			{
				// set the output brightness for the screen
				SetBrightness(ReadSerialData());
				break;
			}
			case CommandCodes::GetPixRect:
			{
				// return a block of pixels from a buffer
				unsigned char x = ReadSerialData();
				unsigned char width = ReadSerialData();
				unsigned char y_height = ReadSerialData();
				unsigned char target = (command_other >> 2) & 0x3;
				WriteSerialData((ResponseCodes::PixRect << 4) | (y_height & 0xF));
				WriteSerialData(width);
				ReadRect(x, (y_height >> 4) & 0xF, width, y_height & 0xF, target == Target::BackBuffer ? g_DisplayReg.BackBuffer : g_DisplayReg.FrontBuffer);
				break;
			}
			case CommandCodes::SolidFill:
			{
				// set a block of pixels in a buffer to a particular value
				unsigned char x = ReadSerialData();
				unsigned char width = ReadSerialData();
				unsigned char y_height = ReadSerialData();
				unsigned char target = (command_other >> 2) & 0x3;
				unsigned char color = command_other & 0x3;
				
				unsigned char *buffer = target == Target::BackBuffer ? g_DisplayReg.BackBuffer : g_DisplayReg.FrontBuffer;
				if(color == 0 && x == 0 && width == BufferWidth && y_height == BufferHeight)
				{
					ClearBuffer(buffer);
				}
				else
				{
					SolidFill(x, (y_height >> 4) & 0xF, width, y_height & 0xF, color, buffer);
				}
				break;
			}
			case CommandCodes::Fill:
			{
				// set a block of pixels in a buffer to the given data
				unsigned char x = ReadSerialData();
				unsigned char width = ReadSerialData();
				unsigned char y_height = ReadSerialData();
				unsigned char target = (command_other >> 2) & 0x3;
				PixelFormat::Enum format = static_cast<PixelFormat::Enum>((command_other >> 1) & 0x1);
				Fill(x, (y_height >> 4) & 0xF, width, y_height & 0xF, format, 
					target == Target::BackBuffer ? g_DisplayReg.BackBuffer : g_DisplayReg.FrontBuffer);
				break;
			}
			case CommandCodes::Copy:
			{
				// copy a block of pixels in a buffer to somewhere else
				unsigned char srcX = ReadSerialData();
				unsigned char dstX = ReadSerialData();
				unsigned char srcY_dstY = ReadSerialData();
				unsigned char width = ReadSerialData();
				unsigned char height_srcTarget_dstTarget = ReadSerialData();
				
				unsigned char *srcBuffer = ((height_srcTarget_dstTarget >> 2) & 0x3) == Target::BackBuffer ? g_DisplayReg.BackBuffer : g_DisplayReg.FrontBuffer;
				unsigned char *dstBuffer = (height_srcTarget_dstTarget & 0x3) == Target::BackBuffer ? g_DisplayReg.BackBuffer : g_DisplayReg.FrontBuffer;
				if(srcX == 0 && dstX == 0 && srcY_dstY == 0 && width == BufferWidth && ((height_srcTarget_dstTarget >> 4) & 0xF) == BufferHeight)
				{
					CopyWholeBuffer(srcBuffer, dstBuffer);
				}
				else
				{
					Copy(srcX, (srcY_dstY >> 4) & 0xF, dstX, srcY_dstY & 0xF, width, (height_srcTarget_dstTarget >> 4) & 0xF, srcBuffer, dstBuffer);
				}
				break;
			}
			case CommandCodes::SetPowerOnImage:
			{
				// set the image to show at startup
				SetPowerOnImage();
				break;
			}
			case CommandCodes::SetHoldTimings:
			{
				// set the hold values for the gray scale bit-planes
				unsigned char b_c = ReadSerialData();
				SetHoldTimings(command_other & 0xF, (b_c >> 4) & 0xF, b_c & 0xF);
				break;
			}
			case CommandCodes::SetIdleTimeout:
			{
				// set the idle time behavior and parameters
				unsigned char timeout = ReadSerialData();
				unsigned char fade = (command_other >> 3) & 0x1;
				unsigned char resetToBootImage = (command_other >> 2) & 0x1;
				SetIdleTimeout(fade, resetToBootImage, timeout);
				break;
			}
			case CommandCodes::GetBufferFullness:
			{
				// respond with the current remaining size for the buffered input
				WriteSerialData(ResponseCodes::BufferState << 4);
				WriteSerialData(GetPendingSerialDataSize());
				break;
			}
		*/
}
