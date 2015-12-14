#include "Eeprom.h"

#include <avr/sfr_defs.h>
#include <avr/io.h>
#include <avr/interrupt.h>
#include <avr/cpufunc.h>

#if defined(__AVR_ATmega88PA__)
	#define EE_PENDING_BUSY EEPE 
	#define EE_FLUSH_DATA   EEMPE
#elif defined(__AVR_ATmega8A__)
	#define EE_PENDING_BUSY EEWE 
	#define EE_FLUSH_DATA   EEMWE
#endif

// Writes a byte to the on chip persistent memory
void WriteEEPROM(unsigned int addr, unsigned char data)
{
	// wait until the memory has finished it's previous operation
	while(EECR & (1 << EE_PENDING_BUSY)) { }
		
	EEAR = addr;
	EEDR = data;
	
	EECR |= (1 << EE_FLUSH_DATA);
	EECR |= (1 << EE_PENDING_BUSY);
}

// Reads a byte from the on chip persistent memory
unsigned char ReadEEPROM(unsigned int addr)
{
	// wait until the memory has finished it's previous operation
	while(EECR & (1 << EE_PENDING_BUSY)) { }
	
	EEAR = addr;
	
	EECR |= (1 << EERE);
	return EEDR;
}
