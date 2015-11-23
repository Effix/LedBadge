
#include "Commands.h"
#include "Display.h"
#include "Buttons.h"
#include "Serial.h"
#include <util/delay_basic.h>

// Power up init
void Setup()
{
	// ports and io
	ConfigurePushButtons();
	ConfigureUART();
	ConfigureDisplay();
	
	sei();
}

// Helper for resyncing after an invalid command is processed
// Similar to the buffer overflow resync, but this can be called from outside of an interrupt handler
static void BadCommandPanic()
{
	// notify of panic state
	WriteSerialData(ResponseCodes::BadCommand << 4);
	
	unsigned char count = 0;
	for(;;)
	{
		// wait for a new byte
		if(ReadSerialData() == 0)
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

int main(void)
{
	Setup();
	
	for(;;)
    {
		// many command parameters in this loop have multiple values packed per-byte
		// these are noted by the underscores in the local vales (and the subsequent shifting and masking)
		
		unsigned char command_other = ReadSerialData();
		switch((command_other >> 4) & 0xF)
		{
			case CommandCodes::Nop:
			{
				// do nothing at all
				break;
			}
			case CommandCodes::Ping:
			{
				// respond with the given cookie
				WriteSerialData((ResponseCodes::Ack << 4) | (command_other & 0xF));
				break;
			}
			case CommandCodes::Version:
			{
				// respond with the version of this firmware
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
			case CommandCodes::SetPix:
			{
				// set a single pixel in a buffer
				unsigned char x = ReadSerialData();
				unsigned char y_target_color = ReadSerialData();
				SetPix(x, (y_target_color >> 4) & 0xF, y_target_color & 0x3, 
					((y_target_color >> 2) & 0x3) == Target::BackBuffer ? g_DisplayReg.BackBuffer : g_DisplayReg.FrontBuffer);
				break;
			}
			case CommandCodes::GetPix:
			{
				// return a single pixel in a buffer
				unsigned char x = ReadSerialData();
				unsigned char y_target = ReadSerialData();
				unsigned char val = GetPix(x, (y_target >> 4) & 0xF, 
					((y_target >> 2) & 0x3) == Target::BackBuffer ? g_DisplayReg.BackBuffer : g_DisplayReg.FrontBuffer);
				WriteSerialData((ResponseCodes::Pix << 4) | val);
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
				SolidFill(x, (y_height >> 4) & 0xF, width, y_height & 0xF, color, 
					target == Target::BackBuffer ? g_DisplayReg.BackBuffer : g_DisplayReg.FrontBuffer);
				break;
			}
			case CommandCodes::Fill:
			{
				// set a block of pixels in a buffer to the given data
				unsigned char x = ReadSerialData();
				unsigned char width = ReadSerialData();
				unsigned char y_height = ReadSerialData();
				unsigned char target = (command_other >> 2) & 0x3;
				Fill(x, (y_height >> 4) & 0xF, width, y_height & 0xF, 
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
				Copy(srcX, (srcY_dstY >> 4) & 0xF, dstX, srcY_dstY & 0xF, width, (height_srcTarget_dstTarget >> 4) & 0xF, 
					((height_srcTarget_dstTarget >> 2) & 0x3) == Target::BackBuffer ? g_DisplayReg.BackBuffer : g_DisplayReg.FrontBuffer,
					(height_srcTarget_dstTarget & 0x3) == Target::BackBuffer ? g_DisplayReg.BackBuffer : g_DisplayReg.FrontBuffer);
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
			default:
			{
				// bad command - probably missed a byte. resync
				BadCommandPanic();
				break;
			}
		}
    }
}
