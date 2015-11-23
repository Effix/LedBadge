#include "Eeprom.h"

#include <avr/sfr_defs.h>
#include <avr/io.h>
#include <avr/interrupt.h>
#include <avr/cpufunc.h>

// Writes a byte to the on chip persistent memory
void WriteEEPROM(unsigned int addr, unsigned char data)
{
	// wait until the memory has finished it's previous operation
	while(EECR & (1 << EEPE)) { }
		
	EEAR = addr;
	EEDR = data;
	
	EECR |= (1 << EEMPE);
	EECR |= (1 << EEPE);
}

// Reads a byte from the on chip persistent memory
unsigned char ReadEEPROM(unsigned int addr)
{
	// wait until the memory has finished it's previous operation
	while(EECR & (1 << EEPE)) { }
	
	EEAR = addr;
	
	EECR |= (1 << EERE);
	return EEDR;
}
