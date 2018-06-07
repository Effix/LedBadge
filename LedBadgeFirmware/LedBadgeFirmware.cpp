
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
		else if(g_CommandReg.AnimPlaying)
		{
			DispatchAnimCommand();
		}
	}


		/*
			case CommandCodes::SetBrightness:
			{
				// set the output brightness for the screen
				SetBrightness(ReadSerialData());
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
		*/
}
