#include "Buttons.h"

void ConfigurePushButtons()
{
	// button inputs are on pc2 and pc3
	DDRC &= ~((1 << PORTC3) | (1 << PORTC2));
	// enable pull ups
	PORTC |= ((1 << PORTC3) | (1 << PORTC2));
}

template<int PIN> static inline bool CheckButton()
{
	static bool buttonDown = false;
	
	if((PINC & (1 << PIN)) == 0)
	{
		if(!buttonDown)
		{
			buttonDown = true;
		}
	}
	else
	{
		buttonDown = false;
	}
	
	return buttonDown;
}

bool CheckButton0()
{
	return CheckButton<PORTC3>();
}

bool CheckButton1()
{
	return CheckButton<PORTC2>();
}
