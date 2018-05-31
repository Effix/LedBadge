#include "I2C.h"
#include <util/twi.h>
#include <util/delay_basic.h>

// Sets up I2C IO
// Called once at program start
void ConfigureI2C()
{
	//PRR &= ~(1 << PRTWI);

	TWSR = 0; // frequency prescaler of 1
#if defined(__AVR_ATmega88PA__)
	TWBR = 0x16; // 200k @ 12MHz
#elif defined(__AVR_ATmega8A__)
	TWBR = 0x0C; // 200k @ 8MHz
#endif
	TWCR = (1 << TWEN);
}

// Blocks until ACK/NACK from slave
void WaitForI2C()
{
	while (!(TWCR & (1 << TWINT))) {}
}

// Sends start signal
void StartI2C()
{
	TWCR = (1 << TWINT) | (1 << TWEN) | (1 << TWSTA);
	WaitForI2C();
}

// Sends stop signal
void StopI2C()
{
	TWCR = (1 << TWINT) | (1 << TWEN) | (1 << TWSTO);
#if defined(__AVR_ATmega88PA__)
	_delay_loop_1(20); // ~60us @ 12MHz
#elif defined(__AVR_ATmega8A__)
	_delay_loop_1(13); // ~60us @ 8MHz
#endif
}

// Begin writing an asynchronous byte, without blocking
void BeginWriteI2C(unsigned char data)
{
	TWDR = data;
	TWCR = (1 << TWINT) | (1 << TWEN);
}

// Finish writing an asynchronous byte, blocking if needed
void EndWriteI2C()
{
	WaitForI2C();
}

// Synchronously write a byte
void WriteI2C(unsigned char data)
{
	BeginWriteI2C(data);
	EndWriteI2C();
}

// Begin reading an asynchronous byte, without blocking
// Set the ack flag to true to continue reading more bytes after this one
void BeginReadI2C(bool ack)
{
	TWCR = (1 << TWINT) | (1 << TWEN) | (ack << TWEA);
}

// Finish reading an asynchronous byte, blocking if needed
unsigned char EndReadI2C()
{
	WaitForI2C();
	return TWDR;
}

// Synchronously read a byte
// Set the ack flag to true to continue reading more bytes after this one
unsigned char ReadI2C(bool ack)
{
	BeginReadI2C(ack);
	return EndReadI2C();
}
