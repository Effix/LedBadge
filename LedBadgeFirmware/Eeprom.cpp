#include "Eeprom.h"

#include <avr/sfr_defs.h>
#include <avr/io.h>
#include <avr/interrupt.h>
#include <avr/cpufunc.h>

void WriteEEPROM(unsigned int addr, unsigned char data)
{
	while(EECR & (1 << EEPE)) { }
		
	EEAR = addr;
	EEDR = data;
	
	EECR |= (1 << EEMPE);
	EECR |= (1 << EEPE);
}

unsigned char ReadEEPROM(unsigned int addr)
{
	while(EECR & (1 << EEPE)) { }
	
	EEAR = addr;
	
	EECR |= (1 << EERE);
	return EEDR;
}
