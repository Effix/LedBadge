#include "Serial.h"
#include "Commands.h"

unsigned char g_SerialBuffer[256] = {};
volatile unsigned char g_SerialReadPos = 0;
volatile unsigned char g_SerialWritePos = 0;

void ConfigureUART()
{
	UBRR0H = 0;
	UBRR0L = 11; // 128800 // 57,600  // 375,000   // 750,000
	UCSR0A |= (1 << U2X0);
	UCSR0B |= (1 << RXEN0) | (1 << TXEN0) | (1 << RXCIE0);
	UCSR0C |= (1 << UCSZ00) | (1 << UCSZ01); // 8 bits, 1 stop bit, 
}

static void OverflowPanic()
{
	WriteSerialData(ResponseCodes::ReceiveOverflow << 4);
	
	unsigned char count = 0;
	for(;;)
	{
		while(!(UCSR0A & (1 << RXC0))) { }
		if(UDR0 == 0)
		{
			if(++count == 0)
			{
				break;
			}
		}
		else
		{
			count = 0;
		}
	}
	
	WriteSerialData(ResponseCodes::Ack << 4);
}

unsigned char ReadSerialData()
{
	while(g_SerialReadPos == g_SerialWritePos) { }
	return g_SerialBuffer[g_SerialReadPos++];
}

void WriteSerialData(unsigned char data)
{
	while(!(UCSR0A & (1 << UDRE0))) { }
	UDR0 = data;
}

ISR(USART_RX_vect, ISR_BLOCK)
{
	g_SerialBuffer[g_SerialWritePos++] = UDR0;
	
	if(g_SerialReadPos == g_SerialWritePos)
	{
		OverflowPanic();
	}
}