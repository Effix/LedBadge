
#include "Commands.h"
#include "Display.h"
#include "Buttons.h"
#include "Serial.h"
#include "I2C.h"
#include "Eeprom.h"

int main(void)
{
	// ports and io
	ConfigureDisplay();
	ConfigurePushButtons();
	ConfigureUART();
	ConfigureI2C();
	ConfigureExternalEEPROM();
	InitAnim();
	
	sei();
	
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
			ResetIdleTime();
		}

		PumpAck();
	}
}
