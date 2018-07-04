#ifndef EEPROM_H_
#define EEPROM_H_

#define ENABLE_EXTERNAL_EEPROM

enum
{
	EepromInternalSize = 0x0200,
	EepromExternalSize = 0x4000
};

// Writes a byte to the on chip persistent memory
void WriteInternalEEPROM(unsigned int addr, unsigned char data);

// Reads a byte from the on chip persistent memory
unsigned char ReadInternalEEPROM(unsigned int addr);

// Initial setup for the off chip memory
void ConfigureExternalEEPROM();

// Soft reset the off chip memory
void ResetExternalEEPROM();

// Begins writing a sequence of bytes to the off chip memory
// This will block while the off chip memory is busy in an internal write state
void BeginWriteExternalEEPROM(unsigned int addr);

// Stuff a byte
void WriteNextByteToExternalEEPROM(unsigned char data, bool moreBytes);

// Do a burst write to the off chip memory
// A maximum of 64 bytes can be written at a time (to a window aligned to a multiple of 64 bytes)
unsigned char WriteExternalEEPROMPage(unsigned int addr, unsigned char count, unsigned char *data);

// Do a burst read from the off chip memory
void ReadExternalEEPROM(unsigned int addr, unsigned char count, unsigned char *data);

// Begins a read sequence from the off chip memory
// This may block if the memory is currently in an internal write state
void BeginReadExternalEEPROM(unsigned int addr);

// Grab a byte
unsigned char ReadNextByteFromExternalEEPROM(bool moreBytes);

#endif /* EEPROM_H_ */