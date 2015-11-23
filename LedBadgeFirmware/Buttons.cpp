#include "Buttons.h"

// Sets up button input
// Called once at program start
void ConfigurePushButtons()
{
	// button inputs are on pc2 and pc3
	DDRC &= ~((1 << PORTC3) | (1 << PORTC2));
	// enable pull ups
	PORTC |= ((1 << PORTC3) | (1 << PORTC2));
}

// Helper for polling the buttons
template<int PIN> static inline bool CheckButton()
{
	// this should so some debouncing logic here...
	return (PINC & (1 << PIN)) == 0;
}

bool CheckButton0()
{
	return CheckButton<PORTC3>();
}

bool CheckButton1()
{
	return CheckButton<PORTC2>();
}
