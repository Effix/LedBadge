#ifndef EEPROM_H_
#define EEPROM_H_

void WriteEEPROM(unsigned int addr, unsigned char data);
unsigned char ReadEEPROM(unsigned int addr);

#endif /* EEPROM_H_ */