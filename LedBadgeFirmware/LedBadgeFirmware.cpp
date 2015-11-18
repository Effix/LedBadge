
#include "Commands.h"
#include "Display.h"
#include "Buttons.h"
#include "Serial.h"
#include <util/delay_basic.h>

//
//
//

void Setup()
{
	// ports and io
	ConfigurePushButtons();
	ConfigureUART();
	ConfigureDisplay();
	
	sei();
}

static void BadCommandPanic()
{
	WriteSerialData(ResponseCodes::BadCommand << 4);
	
	unsigned char count = 0;
	for(;;)
	{
		if(ReadSerialData() == 0)
		{
			if(++count == 0)
			{
				break;
			}
		}
		else
		{
			count = 0;
		}
	}
	
	WriteSerialData(ResponseCodes::Ack << 4);
}

int main(void)
{
	Setup();
	
	for(;;)
    {
		unsigned char command_other = ReadSerialData();
		switch((command_other >> 4) & 0xF)
		{
			case CommandCodes::Nop:
			{
				break;
			}
			case CommandCodes::Ping:
			{
				WriteSerialData((ResponseCodes::Ack << 4) | (command_other & 0xF));
				break;
			}
			case CommandCodes::Version:
			{
				WriteSerialData((ResponseCodes::Version << 4) | VERSION);
				break;
			}
			case CommandCodes::Swap:
			{
				SwapBuffers();
				break;
			}
			case CommandCodes::PollInputs:
			{
				WriteSerialData((ResponseCodes::Inputs << 4) | (CheckButton1() << 1) | CheckButton0());
				break;
			}
			case CommandCodes::SetBrightness:
			{
				SetBrightness(ReadSerialData());
				break;
			}
			case CommandCodes::SetPix:
			{
				unsigned char x = ReadSerialData();
				unsigned char y_target_color = ReadSerialData();
				SetPix(x, (y_target_color >> 4) & 0xF, y_target_color & 0x3, 
					((y_target_color >> 2) & 0x3) == Target::BackBuffer ? g_DisplayReg.BackBuffer : g_DisplayReg.FrontBuffer);
				break;
			}
			case CommandCodes::GetPix:
			{
				unsigned char x = ReadSerialData();
				unsigned char y_target = ReadSerialData();
				unsigned char val = GetPix(x, (y_target >> 4) & 0xF, 
					((y_target >> 2) & 0x3) == Target::BackBuffer ? g_DisplayReg.BackBuffer : g_DisplayReg.FrontBuffer);
				WriteSerialData((ResponseCodes::Pix << 4) | val);
				break;
			}
			case CommandCodes::GetPixRect:
			{
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
				SetPowerOnImage();
				break;
			}
			case CommandCodes::SetHoldTimings:
			{
				unsigned char b_c = ReadSerialData();
				SetHoldTimings(command_other & 0xF, (b_c >> 4) & 0xF, b_c & 0xF);
				break;
			}
			default:
			{
				BadCommandPanic();
				break;
			}
		}
    }
}
