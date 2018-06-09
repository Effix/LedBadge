#include "Serial.h"
#include "Commands.h"
#include "Display.h"
#include <util/atomic.h>

// Circular read buffer that the input interrupt can fill out while pixels are being pushed out
static unsigned char g_SerialBuffer[256] __attribute__ ((section (".serialBuffer")));
volatile unsigned char g_SerialReadPos = 0;
volatile unsigned char g_SerialWritePos = 0;
volatile unsigned char g_SerialCount = 0;

#if defined(__AVR_ATmega88PA__)
	#define UR_CTRL_REG_A	UCSR0A
	#define UR_RX_COMPLETE	RXC0
	#define UR_DATA_BUFFER	UDR0
	#define UR_DATA_EMPTY	UDRE0
	#define UR_RX_vect		USART_RX_vect
#elif defined(__AVR_ATmega8A__)
	#define UR_CTRL_REG_A	UCSRA
	#define UR_RX_COMPLETE	RXC
	#define UR_DATA_BUFFER	UDR
	#define UR_DATA_EMPTY	UDRE
	#define UR_RX_vect		USART_RXC_vect
#endif

// Sets up serial IO
// Called once at program start
void ConfigureUART()
{
	//PRR &= ~(1 << PRUSART0);

#if defined(__AVR_ATmega88PA__)
	UBRR0H = 0;
	UBRR0L = 12; // 115200 baud ~ -0.17% error
	UCSR0A = (1 << U2X0);
	UCSR0B = (1 << RXEN0) | (1 << TXEN0) | (1 << RXCIE0);
	UCSR0C = (1 << UCSZ00) | (1 << UCSZ01); // 8 bits, 1 stop bits
#elif defined(__AVR_ATmega8A__)
	OSCCAL = 0xAD; // calibrate to 8mhz
	UBRRH = 0;
	UBRRL = 25; // 38400 baud ~ -0.1667% error
	UCSRA = (1 << U2X);
	UCSRB = (1 << RXEN) | (1 << TXEN) | (1 << RXCIE);
	UCSRC = (1 << URSEL) | (1 << UCSZ0) | (1 << UCSZ1); // 8 bits, 1 stop bits 
#endif
}

// Handles the re-sync protocol (write panic -> read 255 0xFF -> write ok)
// Must be called with interrupts disabled (or within the UART interrupt handler)
static void OverflowPanic()
{
	// avoid an irritating bright segment wile this is all happening
	EnableDisplay(false);
	
	// notify of panic state
	WriteSerialData((ResponseCodes::Error << 4) | ErrorCodes::ReceiveBufferOverrun);
	
	unsigned char count = 0;
	for(;;)
	{
		// wait for a new byte
		while(!(UR_CTRL_REG_A & (1 << UR_RX_COMPLETE))) { }
		if(UR_DATA_BUFFER == 0xFF)
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
	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
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
	while(!(UR_CTRL_REG_A & (1 << UR_DATA_EMPTY))) { }
	UR_DATA_BUFFER = data;
}

// Gets the total number of bytes that can be read without blocking
unsigned char GetPendingSerialDataSize()
{
	unsigned char count;
	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		count = g_SerialCount;
	}
	return count;
}

void PumpAck()
{
	// TODO: 
}

// Interrupt handler for incoming IO
// Shovels data into the circular read buffer
ISR(UR_RX_vect, ISR_BLOCK)
{
	g_SerialBuffer[g_SerialWritePos++] = UR_DATA_BUFFER;
	++g_SerialCount;

	// uh oh... we caught up to the beginning of the buffer and have overwritten something
	if(g_SerialReadPos == g_SerialWritePos)
	{
		OverflowPanic();
	}
}
