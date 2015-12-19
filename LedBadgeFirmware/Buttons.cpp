#include "Buttons.h"

#include <avr/io.h>

// Sets up button input
// Called once at program start
void ConfigurePushButtons()
{
	// button input direction
#if defined(__AVR_ATmega88PA__)
	DDRC &= ~((1 << DDC3) | (1 << DDC2));
#elif defined(__AVR_ATmega8A__)
	DDRB &= ~(1 << DDB3);
	DDRD &= ~(1 << DDD2);
#endif

	// enable pull ups
#if defined(__AVR_ATmega88PA__)
	PORTC |= (1 << PORTC3) | (1 << PORTC2);
#elif defined(__AVR_ATmega8A__)
	PORTB |= (1 << PORTB3);
	PORTD |= (1 << PORTD2);
#endif
}

bool CheckButton0()
{
	// need debouncing logic? it is only polled on demand and not constantly...
#if defined(__AVR_ATmega88PA__)
	return (PINC & (1 << PINC3)) == 0;
#elif defined(__AVR_ATmega8A__)
	return (PINB & (1 << PINB3)) == 0;
#endif
}

bool CheckButton1()
{
	// need debouncing logic? it is only polled on demand and not constantly...
#if defined(__AVR_ATmega88PA__)
	return (PINC & (1 << PINC2)) == 0;
#elif defined(__AVR_ATmega8A__)
	return (PIND & (1 << PIND2)) == 0;
#endif
}
