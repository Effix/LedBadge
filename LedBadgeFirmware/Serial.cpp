#include "Serial.h"
#include "Commands.h"
#include "Display.h"
#include <util/atomic.h>

// Circular read buffer that the input interrupt can fill out while pixels are being pushed out
unsigned char g_SerialBuffer[256] = {};
volatile unsigned char g_SerialReadPos = 0;
volatile unsigned char g_SerialWritePos = 0;
volatile unsigned char g_SerialCount = 0;

// Sets up serial IO
// Called once at program start
void ConfigureUART()
{
	UBRR0H = 0;
	UBRR0L = 11; // 128800 baud
	UCSR0A |= (1 << U2X0);
	UCSR0B |= (1 << RXEN0) | (1 << TXEN0) | (1 << RXCIE0);
	UCSR0C |= (1 << UCSZ00) | (1 << UCSZ01); // 8 bits, 1 stop bit, 
}

// Handles the resync protocol (write panic -> read 255 zeros -> write ok)
// Must be called with interrupts disabled (or within the UART interrupt handler)
static void OverflowPanic()
{
	// avoid an irritating bright segment wile this is all happening
	EnableDisplay(false);
	
	// notify of panic state
	WriteSerialData(ResponseCodes::ReceiveOverflow << 4);
	
	unsigned char count = 0;
	for(;;)
	{
		// wait for a new byte
		while(!(UCSR0A & (1 << RXC0))) { }
		if(UDR0 == 0)
		{
			// gradually reset the buffered data while we are getting all of these 0s
			g_SerialBuffer[count] = 0;
			
			// check for the full sequence of nops
			if(++count == 0)
			{
				// finished!
				break;
			}
		}
		else
		{
			// probably still in the middle of frame data... start over
			count = 0;
		}
	}
	
	// The read buffer is totally zeroed out, so leave it with about a frame buffer's worth of data to read in case we were in the middle of a fill
	g_SerialReadPos = 0;
	g_SerialWritePos = 
	g_SerialCount = BufferPackedLength; 
	
	// ok, all resynchronized
	WriteSerialData(ResponseCodes::Ack << 4);
	
	// restore image
	EnableDisplay(true);
}

// Read a byte from the serial port
// Will block if the input buffer is empty
unsigned char ReadSerialData()
{
	// wait until some data is in the ring buffer
	while(g_SerialReadPos == g_SerialWritePos) { }
	
	unsigned char data;	
	ATOMIC_BLOCK(ATOMIC_FORCEON)
	{
		--g_SerialCount;
		data = g_SerialBuffer[g_SerialReadPos++];
	}
	
	return data;
}

// Write a byte to the serial port
// Will block if the output buffer is full
void WriteSerialData(unsigned char data)
{
	// Wait for the output buffer to free up
	while(!(UCSR0A & (1 << UDRE0))) { }
	UDR0 = data;
}

// Gets the total number of bytes that can be read without blocking
unsigned char GetPendingSerialDataSize()
{
	unsigned char count;
	ATOMIC_BLOCK(ATOMIC_FORCEON)
	{
		count = g_SerialCount;
	}
	return count;
}

// Interrupt handler for incoming IO
// Shovels data into the circular read buffer
ISR(USART_RX_vect, ISR_BLOCK)
{
	g_SerialBuffer[g_SerialWritePos++] = UDR0;
	++g_SerialCount;

	// uh oh... we caught up to the beginning of the buffer and have overwritten something
	if(g_SerialReadPos == g_SerialWritePos)
	{
		OverflowPanic();
	}
}
