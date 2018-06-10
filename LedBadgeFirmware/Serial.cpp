#include "Serial.h"
#include "Commands.h"
#include "Display.h"
#include <util/atomic.h>
#include <util/crc16.h>

enum
{
	AckBufferSize = 8,
	AckPacketFlag = (0)
};

struct SerialState 
{
	enum Enum
	{
		Waiting,
		Header,
		Body
	};
};

struct PacketHeader
{
	unsigned char Cookie;
	unsigned char Length;
	unsigned int PacketCRC;
};

struct PendingAck
{
	unsigned char Header;
	unsigned char Cookie;
};

// Circular read buffer that the input interrupt can fill out while pixels are being pushed out
// Transactioned with a pending write-position/size
static unsigned char g_SerialBuffer[256] __attribute__ ((section (".serialBuffer")));
static volatile unsigned char g_SerialReadPos = 0;
static volatile unsigned char g_SerialWritePos = 0;
static volatile unsigned char g_SerialPendingWritePos = 0;
static volatile unsigned char g_SerialCount = 0;
static volatile unsigned char g_SerialPendingCount = 0;

// Circular buffer for responses - pushed from the serial interrupt, sent from the main thread
static PendingAck g_SerialAckQueue[AckBufferSize];
static volatile unsigned char g_SerialAckReadPos = 0;
static volatile unsigned char g_SerialAckWritePos = 0;

static SerialState::Enum g_SerialState = SerialState::Waiting;
static union
{
	PacketHeader Header;
	unsigned char Buffer[4];
} g_SerialPacketHeader;
static unsigned char g_SerialPacketHeaderBufferPos = 0;
static unsigned char g_SerialHeaderRunningCRC = 0;
static unsigned int g_SerialRunningCRC = 0;

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

// Read a byte from the serial port
// Will block if the input buffer is empty
unsigned char ReadSerialData()
{
	// wait until some data is in the ring buffer
	while(g_SerialReadPos == g_SerialWritePos) 
	{
		PumpAck();
	}
	
	unsigned char data;	
	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		--g_SerialCount;
		--g_SerialPendingCount;
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

// Send along 
void PumpAck()
{
	if(g_SerialAckReadPos != g_SerialAckWritePos)
	{
		WriteSerialData(g_SerialAckQueue[g_SerialAckReadPos].Header);
		WriteSerialData(g_SerialAckQueue[g_SerialAckReadPos].Cookie);
		g_SerialAckReadPos = (g_SerialAckReadPos + 1) & (AckBufferSize - 1);
	}
}

// Interrupt handler for incoming IO
// Shovels data into the circular read buffer
ISR(UR_RX_vect, ISR_BLOCK)
{
	switch(g_SerialState)
	{
		case SerialState::Waiting:
		{
			if(UR_DATA_BUFFER == 0xA5)
			{
				g_SerialHeaderRunningCRC = UR_DATA_BUFFER;
				g_SerialPacketHeaderBufferPos = 0;
				g_SerialState = SerialState::Header;
			}
			break;
		}
		case SerialState::Header:
		{
			if(g_SerialPacketHeaderBufferPos == 4)
			{
				if(UR_DATA_BUFFER == g_SerialHeaderRunningCRC)
				{
					if(g_SerialPacketHeader.Header.Length)
					{
						// get ready for data
						g_SerialRunningCRC = 0xFFFF;
						g_SerialPendingWritePos = g_SerialWritePos;
						g_SerialState = SerialState::Body;
					}
					else
					{
						// handle the case of an empty packet
						if(g_SerialPacketHeader.Header.Cookie)
						{
							g_SerialAckQueue[g_SerialAckWritePos].Header = (ResponseCodes::Ack << 4) | AckPacketFlag;
							g_SerialAckQueue[g_SerialAckWritePos++].Cookie = g_SerialPacketHeader.Header.Cookie;
						}
						g_SerialState = SerialState::Waiting;
					}
				}
				else
				{
					// failure - send notification and reset
					g_SerialAckQueue[g_SerialAckWritePos].Header = (ResponseCodes::Error << 4) | ErrorCodes::CorruptPacketHeader;
					g_SerialAckQueue[g_SerialAckWritePos++].Cookie = 0;
					g_SerialState = SerialState::Waiting;
				}
			}
			else
			{
				g_SerialPacketHeader.Buffer[g_SerialPacketHeaderBufferPos++] = UR_DATA_BUFFER;
				g_SerialHeaderRunningCRC = _crc8_ccitt_update(g_SerialHeaderRunningCRC, UR_DATA_BUFFER);
			}
			break;
		}
		case SerialState::Body:
		{
			if(g_SerialPendingCount != ~0)
			{
				g_SerialBuffer[g_SerialPendingWritePos++] = UR_DATA_BUFFER;
				g_SerialRunningCRC = _crc_ccitt_update(g_SerialRunningCRC, UR_DATA_BUFFER);
				++g_SerialPendingCount;

				// commit, if we have completed the data transfer
				if(--g_SerialPacketHeader.Header.Length == 0)
				{
					if(g_SerialRunningCRC == g_SerialPacketHeader.Header.PacketCRC)
					{
						g_SerialWritePos = g_SerialPendingWritePos;
						g_SerialCount = g_SerialPendingCount;

						// notify of success
						if(g_SerialPacketHeader.Header.Cookie)
						{
							g_SerialAckQueue[g_SerialAckWritePos].Header = (ResponseCodes::Ack << 4) | AckPacketFlag;
							g_SerialAckQueue[g_SerialAckWritePos++].Cookie = g_SerialPacketHeader.Header.Cookie;
						}
					}
					else
					{
						// failure!
						g_SerialAckQueue[g_SerialAckWritePos].Header = (ResponseCodes::Error << 4) | ErrorCodes::CorruptPacketData;
						g_SerialAckQueue[g_SerialAckWritePos++].Cookie = g_SerialPacketHeader.Header.Cookie;
					}
					g_SerialState = SerialState::Waiting;
				}
			}
			else
			{
				// this would have overflowed
				g_SerialAckQueue[g_SerialAckWritePos].Header = (ResponseCodes::Error << 4) | ErrorCodes::ReceiveBufferOverrun;
				g_SerialAckQueue[g_SerialAckWritePos++].Cookie = g_SerialPacketHeader.Header.Cookie;
				g_SerialState = SerialState::Waiting;
			}
			break;
		}
	}
}
