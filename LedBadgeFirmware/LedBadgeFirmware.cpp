
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
		else
		{
			PumpAck();
		}
	}
}
