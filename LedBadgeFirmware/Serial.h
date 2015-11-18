#ifndef SERIAL_H_
#define SERIAL_H_

#include <avr/sfr_defs.h>
#include <avr/io.h>
#include <avr/interrupt.h>
#include <avr/cpufunc.h>

void ConfigureUART();

unsigned char ReadSerialData();
void WriteSerialData(unsigned char data);

#endif /* SERIAL_H_ */