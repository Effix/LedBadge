#ifndef EEPROM_H_
#define EEPROM_H_

// Writes a byte to the on chip persistent memory
void WriteEEPROM(unsigned int addr, unsigned char data);

// Reads a byte from the on chip persistent memory
unsigned char ReadEEPROM(unsigned int addr);

#endif /* EEPROM_H_ */