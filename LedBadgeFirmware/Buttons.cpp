#include "Buttons.h"

#include <avr/io.h>

#if defined(__AVR_ATmega88PA__)
	#define BTN_PORT_CFG_0        DDRC   
	#define BTN_PORT_CFG_1        DDRC   
	#define BTN_PORT_CFG_PIN_0    DDC3   
	#define BTN_PORT_CFG_PIN_1    DDC2   
	#define BTN_PORT_PULLUP_0     PORTC  
	#define BTN_PORT_PULLUP_1     PORTC  
	#define BTN_PORT_PULLUP_PIN_0 PORTC3 
	#define BTN_PORT_PULLUP_PIN_1 PORTC2 
	#define BTN_PORT_IO_0         PINC   
	#define BTN_PORT_IO_1         PINC   
	#define BTN_PORT_IO_PIN_0     PINC3  
	#define BTN_PORT_IO_PIN_1     PINC2  
#elif defined(__AVR_ATmega8A__)
	#define BTN_PORT_CFG_0        DDRB   
	#define BTN_PORT_CFG_1        DDRD   
	#define BTN_PORT_CFG_PIN_0    DDB3   
	#define BTN_PORT_CFG_PIN_1    DDD2   
	#define BTN_PORT_PULLUP_0     PORTB  
	#define BTN_PORT_PULLUP_1     PORTD  
	#define BTN_PORT_PULLUP_PIN_0 PORTB3 
	#define BTN_PORT_PULLUP_PIN_1 PORTD2 
	#define BTN_PORT_IO_0         PINB   
	#define BTN_PORT_IO_1         PIND   
	#define BTN_PORT_IO_PIN_0     PINB3  
	#define BTN_PORT_IO_PIN_1     PIND2  
#endif

// Sets up button input
// Called once at program start
void ConfigurePushButtons()
{
	// button input direction
	BTN_PORT_CFG_0 &= ~(1 << BTN_PORT_CFG_PIN_0);
	BTN_PORT_CFG_1 &= ~(1 << BTN_PORT_CFG_PIN_1);
	// enable pull ups
	BTN_PORT_PULLUP_0 |= (1 << BTN_PORT_PULLUP_PIN_0);
	BTN_PORT_PULLUP_1 |= (1 << BTN_PORT_PULLUP_PIN_1);
}

bool CheckButton0()
{
	// need debouncing logic? it is only polled on demand and not constantly...
	return (BTN_PORT_IO_0 & (1 << BTN_PORT_IO_PIN_0)) == 0;
}

bool CheckButton1()
{
	// need debouncing logic? it is only polled on demand and not constantly...
	return (BTN_PORT_IO_1 & (1 << BTN_PORT_IO_PIN_1)) == 0;
}
