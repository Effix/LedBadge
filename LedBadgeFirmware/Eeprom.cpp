#include "Eeprom.h"

#include "I2C.h"
#include <avr/sfr_defs.h>
#include <avr/io.h>
#include <avr/interrupt.h>
#include <avr/cpufunc.h>
#include <util/atomic.h>
#include <util/twi.h>

#if defined(__AVR_ATmega88PA__)
	#define EE_PENDING_BUSY EEPE 
	#define EE_FLUSH_DATA   EEMPE
#elif defined(__AVR_ATmega8A__)
	#define EE_PENDING_BUSY EEWE 
	#define EE_FLUSH_DATA   EEMWE
#endif

#define EEPROM_I2C_ADDR		0xA0

// Writes a byte to the on chip persistent memory
void WriteInternalEEPROM(unsigned int addr, unsigned char data)
{
	// wait until the memory has finished it's previous operation
	while(EECR & (1 << EE_PENDING_BUSY)) { }
		
	EEAR = addr;
	EEDR = data;
	
	ATOMIC_BLOCK(ATOMIC_FORCEON)
	{
		EECR |= (1 << EE_FLUSH_DATA);
		EECR |= (1 << EE_PENDING_BUSY);
	}
}

// Reads a byte from the on chip persistent memory
unsigned char ReadInternalEEPROM(unsigned int addr)
{
	// wait until the memory has finished it's previous operation
	while(EECR & (1 << EE_PENDING_BUSY)) { }
	
	EEAR = addr;
	
	EECR |= (1 << EERE);
	return EEDR;
}

// Initial setup for the off chip memory
void ConfigureExternalEEPROM()
{
#ifdef ENABLE_EXTERNAL_EEPROM
	// enable internal pull-ups
	DDRC &= ~((1 << DDC4) | (1 << DDC5));
	PORTC |= (1 << PORTC4) | (1 << PORTC5);
#if defined(__AVR_ATmega88PA__)
	MCUCR &= ~(1 << PUD);
#elif defined(__AVR_ATmega8A__)
	SFIOR &= ~(1 << PUD); 
#endif

	ResetExternalEEPROM();
#endif
}

// Soft reset the off chip memory
void ResetExternalEEPROM()
{
	/*StartI2C();
	WriteI2C(0xFF);
	StartI2C();
	StopI2C();*/
}

// Begins writing a sequence of bytes to the off chip memory
// This will block while the off chip memory is busy in an internal write state
void BeginWriteExternalEEPROM(unsigned int addr)
{
	for(;;) // need to keep polling this if it doesn't respond; it may be in an internal write cycle
	{
		StartI2C();
		WriteI2C(EEPROM_I2C_ADDR | TW_WRITE);
		if(TW_STATUS != TW_MT_SLA_NACK)
		{
			WriteI2C((addr >> 8) & 0xFF);
			WriteI2C(addr & 0xFF);
			break;
		}
		StopI2C();
	}
}

// Stuff a byte
void WriteNextByteToExternalEEPROM(unsigned char data, bool moreBytes)
{
	WriteI2C(data);
	if(!moreBytes)
	{
		StopI2C();
	}
}

// Do a burst write to the off chip memory
// A maximum of 64 bytes can be written at a time (to a window aligned to a multiple of 64 bytes)
unsigned char WriteExternalEEPROMPage(unsigned int addr, unsigned char count, unsigned char *data)
{
	unsigned char maxWrite = 64 - (count & 63);
	if (count > maxWrite)
	{
		count = maxWrite;
	}
	unsigned char written = count;

	BeginWriteExternalEEPROM(addr);
	while(count--)
	{
		WriteNextByteToExternalEEPROM(*data++, count > 0);
	}

	return written;
}

// Do a burst read from the off chip memory
void ReadExternalEEPROM(unsigned int addr, unsigned char count, unsigned char *data)
{
	BeginReadExternalEEPROM(addr);
	while(count--)
	{
		*data++ = ReadNextByteFromExternalEEPROM(count != 0);
	}
}

// Begins a read sequence from the off chip memory
// This may block if the memory is currently in an internal write state
void BeginReadExternalEEPROM(unsigned int addr)
{
	BeginWriteExternalEEPROM(addr); // dummy write is required to seek the read cursor
	StartI2C();
	WriteI2C(EEPROM_I2C_ADDR | TW_READ);
}

// Grab a byte
unsigned char ReadNextByteFromExternalEEPROM(bool moreBytes)
{
	BeginReadI2C(moreBytes);
	unsigned char c = EndReadI2C();
	if(!moreBytes)
	{
		StopI2C();
	}
	return c;
}
