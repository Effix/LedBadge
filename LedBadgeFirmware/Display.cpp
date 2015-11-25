#include "Display.h"
#include "Serial.h"
#include "Eeprom.h"

// Segment order from upper left to lower right
// This isn't used directly, it is inlined in the jumptable below
/*const unsigned char g_RowSwizzleTable[BufferHeight * 2] = 
{
	11, 7, 
	10, 6, 
	9,  5, 
	8,  4,
	23, 3,
	22, 2,
	21, 1,
	20, 0,
	19, 15,
	18, 14,
	17, 13,
	16, 12
};*/

// Scrambles the output row selection to reduce the perceived flicker on the display
const unsigned char g_RowDitherTable[BufferHeight] = 
{
	 7,
	 2,
	 9,
	 0,
	11,
	 4,
	 6,
	 8,
	 1,
	10,
	 3,
	 5
};

// Gamma ramp for converting input brightness to pwm ratio
const unsigned char g_BrightnessTable[BrightnessLevels] PROGMEM = 
{
	0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
	0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 
	0x01, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x03, 0x03, 0x03, 0x03, 0x03, 0x04, 0x04, 
	0x04, 0x04, 0x04, 0x05, 0x05, 0x05, 0x05, 0x06, 0x06, 0x06, 0x06, 0x07, 0x07, 0x07, 0x07, 0x08, 
	0x08, 0x08, 0x09, 0x09, 0x09, 0x0A, 0x0A, 0x0A, 0x0B, 0x0B, 0x0C, 0x0C, 0x0C, 0x0D, 0x0D, 0x0E, 
	0x0E, 0x0F, 0x0F, 0x0F, 0x10, 0x10, 0x11, 0x11, 0x12, 0x12, 0x13, 0x13, 0x14, 0x14, 0x15, 0x16, 
	0x16, 0x17, 0x17, 0x18, 0x19, 0x19, 0x1A, 0x1A, 0x1B, 0x1C, 0x1C, 0x1D, 0x1E, 0x1E, 0x1F, 0x20, 
	0x21, 0x21, 0x22, 0x23, 0x24, 0x24, 0x25, 0x26, 0x27, 0x28, 0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 
	0x2E, 0x2E, 0x2F, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 
	0x3D, 0x3E, 0x3F, 0x40, 0x41, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4B, 0x4C, 0x4D, 0x4E, 
	0x50, 0x51, 0x52, 0x53, 0x55, 0x56, 0x57, 0x59, 0x5A, 0x5B, 0x5D, 0x5E, 0x5F, 0x61, 0x62, 0x63, 
	0x65, 0x66, 0x68, 0x69, 0x6B, 0x6C, 0x6E, 0x6F, 0x71, 0x72, 0x74, 0x75, 0x77, 0x79, 0x7A, 0x7C, 
	0x7D, 0x7F, 0x81, 0x82, 0x84, 0x86, 0x87, 0x89, 0x8B, 0x8D, 0x8E, 0x90, 0x92, 0x94, 0x96, 0x97, 
	0x99, 0x9B, 0x9D, 0x9F, 0xA1, 0xA3, 0xA5, 0xA6, 0xA8, 0xAA, 0xAC, 0xAE, 0xB0, 0xB2, 0xB4, 0xB6, 
	0xB8, 0xBA, 0xBD, 0xBF, 0xC1, 0xC3, 0xC5, 0xC7, 0xC9, 0xCC, 0xCE, 0xD0, 0xD2, 0xD4, 0xD7, 0xD9, 
	0xDB, 0xDD, 0xE0, 0xE2, 0xE4, 0xE7, 0xE9, 0xEB, 0xEE, 0xF0, 0xF3, 0xF5, 0xF8, 0xFA, 0xFD, 0xFF
};

// Display state machine values
DisplayState g_DisplayReg = {};

// Set a block of pixels in a buffer to a particular value
void SolidFill(unsigned char x, unsigned char y, unsigned char width, unsigned char height, unsigned char color, unsigned char *buffer)
{
	for(unsigned char iy = y, sy = y + height; iy < sy; ++iy)
	{
		for(unsigned char ix = x, sx = x + width; ix < sx; ++ix)
		{
			SetPix(ix, iy, color, buffer);
		}
	}
}

// Set a block of pixels in a buffer to the given data (read from the serial port)
void Fill(unsigned char x, unsigned char y, unsigned char width, unsigned char height, unsigned char *buffer)
{
	unsigned char data = 0;
	unsigned char rem = 0;
	
	for(unsigned char iy = y, sy = y + height; iy < sy; ++iy)
	{
		for(unsigned char ix = x, sx = x + width; ix < sx; ++ix)
		{
			if(!rem)
			{
				data = ReadSerialData();
				rem = 4;
			}
			
			SetPix(ix, iy, data & 0x3, buffer);
			data >>= 2;
			--rem;
		}
	}
}

// Copy a block of pixels in a buffer to somewhere else
void Copy(unsigned char srcX, unsigned char srcY, unsigned char dstX, unsigned char dstY, unsigned char width, unsigned char height, unsigned char *srcBuffer, unsigned char *dstBuffer)
{
	for(unsigned char sy = srcY, dy = dstY, ey = srcY + height; sy < ey; ++sy, ++dy)
	{
		for(unsigned char sx = srcX, dx = dstX, ex = srcX + width; sx < ex; ++sx, ++dx)
		{
			SetPix(dx, dy, GetPix(sx, sy, srcBuffer), dstBuffer);
		}
	}
}

// Return a block of pixels from a buffer (sending it out to the serial port, 2bpp packed)
void ReadRect(unsigned char x, unsigned char  y, unsigned char width, unsigned char height, unsigned char *buffer)
{
	unsigned char data = 0;
	unsigned char read = 0;
	
	for(unsigned char iy = y, sy = y + height; iy < sy; ++iy)
	{
		for(unsigned char ix = x, sx = x + width; ix < sx; ++ix)
		{
			unsigned char val = GetPix(ix, iy, buffer);
			data |= val << (read << 1);
			read++;
			
			if(read == 4)
			{
				WriteSerialData(data);
				data = 0;
				read = 0;
			}
		}
	}
	
	if(read)
	{
		WriteSerialData(data);
	}
}

// Set the image to show at startup (saves the front buffer to non-volatile memory)
void SetPowerOnImage()
{
	cli();
	
	// turn off the output to avoid a bright segment just hanging out
	EnableDisplay(false);
	
	unsigned char *p = g_DisplayReg.FrontBuffer;
	for(unsigned char i = 0; i < BufferLength; ++i, ++p)
	{
		WriteEEPROM(i, *p);
	}
	
	// restore image
	EnableDisplay(true);
	
	sei();
}

// Fills the font buffer directly from the on chip non-volatile memory
// Must be called with interrupts disabled
void LoadPowerOnImage()
{
	unsigned char *p = g_DisplayReg.FrontBuffer;
	for(unsigned char i = 0; i < BufferLength; ++i, ++p)
	{
		*p = ReadEEPROM(i);
	}
}

// Flips the front and back buffers (latches over at the end of the frame)
void SwapBuffers()
{
	g_DisplayReg.SwapRequest = true;
	while(g_DisplayReg.SwapRequest) {}
}

// Commits the requested buffer swap at the end of the frame
static inline void LatchInFrameSwap()
{
	if(g_DisplayReg.SwapRequest)
	{
		g_DisplayReg.SwapRequest = false;
		
		g_DisplayReg.BufferSelect = !g_DisplayReg.BufferSelect;
		g_DisplayReg.FrontBuffer = g_DisplayReg.Buffers[g_DisplayReg.BufferSelect];
		g_DisplayReg.BackBuffer = g_DisplayReg.Buffers[!g_DisplayReg.BufferSelect];
	}
}

// Sets the overall image brightness (latches over at the end of the frame)
void SetBrightness(unsigned char brightness)
{
	g_DisplayReg.BrightnessLevel = brightness;
	g_DisplayReg.ChangeBrightnessRequest = true;
}

// Set the hold values for the gray scale bit-planes
// Values are differential and the brightnesses are effectively a, a+b, and a+b+c 
// So, in order to get a 1, 5, 9 spread, you would pass in a=1, b=4, c=4
void SetHoldTimings(unsigned char a, unsigned char b, unsigned char c)
{
	g_DisplayReg.GammaTable[0] = a;
	g_DisplayReg.GammaTable[1] = b;
	g_DisplayReg.GammaTable[2] = c;
}

// Modifies the overall display brightness
static inline void SetBrightnessLevelRegisters(unsigned char level)
{
	// the OE (output enable) signal is wired up to one of the pwm pins, so this has way more intensity levels than we can generate with the gray scale bit-planes
	// since it is a separate pin, it overlays nicely, but dim values can end up looking a little flickery
	if(level)
	{
		// grab the pwm duty cycle from the look up table
		TCCR0B |= (1 << CS00);
		OCR0B = pgm_read_byte(&g_BrightnessTable[level]);
	}
	else
	{
		// ...or just totally off
		TCCR0B &= ~(1 << CS00);
		OCR0B = ~0;
		TCNT0 = 0;
	}
}

// Commits the requested brightness change when it is safe to do so
static inline void LatchInBrightness()
{
	if(g_DisplayReg.ChangeBrightnessRequest)
	{
		g_DisplayReg.ChangeBrightnessRequest = false;
		SetBrightnessLevelRegisters(g_DisplayReg.BrightnessLevel);
	}
}

// Sets up the ports bound to the led drivers configures the output state, and fills the front buffer with the startup image
// Called once at program start
void ConfigureDisplay()
{
	// data and clock pins
	DDRB |= (1 << PORTB1) | (1 << PORTB0);
	PORTB &= ~((1 << PORTB1) | (1 << PORTB0));
	DDRD |= (1 << PORTD7) | (1 << PORTD6) | (1 << PORTD5);
	PORTD &= ~((1 << PORTD7) | (1 << PORTD6) | (1 << PORTD5));
	
	// brightness pwm timer
	TCCR0A |= (1 << COM0B0) | (1 << COM0B1) | (1 << WGM00) | (1 << WGM01);
	TCCR0B |= (1 << CS00);
	OCR0B = BrightnessLevels / 2;
	
	// refresh timer
	TCCR2B |= (1 << CS21);
	OCR2A = 344 / 8;
	TIMSK2 |= (1 << OCIE2A);
	
	g_DisplayReg.FrontBuffer = g_DisplayReg.Buffers[0];
	g_DisplayReg.BackBuffer = g_DisplayReg.Buffers[1];
	g_DisplayReg.BrightnessLevel = BrightnessLevels / 2;
	g_DisplayReg.GammaTable[0] = 1;
	g_DisplayReg.GammaTable[1] = 4;
	g_DisplayReg.GammaTable[2] = 4;
	g_DisplayReg.Y = BufferHeight - 1;
	g_DisplayReg.Half = 1;
	g_DisplayReg.BitPlane = BufferBitPlanes - 1;
	g_DisplayReg.BitPlaneHold = g_DisplayReg.GammaTable[g_DisplayReg.BitPlane];
	g_DisplayReg.BufferP = g_DisplayReg.FrontBuffer + BufferLength;
	
	LoadPowerOnImage();
}

// Helper for blanking out the display during long operations while interrupts are disabled
void EnableDisplay(bool enable)
{
	if(enable)
	{
		SetBrightnessLevelRegisters(g_DisplayReg.BrightnessLevel);
	}
	else
	{
		SetBrightnessLevelRegisters(0);
	}
}

// Updates one segment of the display (one half a a row)
inline void RefreshDisplay()
{
	unsigned char y = g_RowDitherTable[g_DisplayReg.Y];
	unsigned char row = (y << 1) + g_DisplayReg.Half;
	g_DisplayReg.BufferP = g_DisplayReg.FrontBuffer + 
		(g_DisplayReg.BitPlane * BufferBitPlaneLength) + 
		(y * BufferBitPlaneStride) + 
		(g_DisplayReg.Half == 0 ? BufferBitPlaneStride / 2 : BufferBitPlaneStride);

	unsigned char portB = PORTB & ~(1 << PORTB0); // shift register clock low
	unsigned char portD_default = PORTD | (1 << PORTD7); // row select high
	unsigned char portD_selectRow = PORTD & ~(1 << PORTD7); // row select low

	// unrolled swizzle lookup+shifting out of the pixel values
	PORTD = portD_default;
	switch(row)
	{
		case  0:
		{
			#define SELECT_ROW 11
			#include "ClockOutPixels.h"
			break;
		}
		case  1:
		{
			#define SELECT_ROW  7
			#include "ClockOutPixels.h"
			break;
		}
		case  2:
		{
			#define SELECT_ROW 10
			#include "ClockOutPixels.h"
			break;
		}
		case  3:
		{
			#define SELECT_ROW  6
			#include "ClockOutPixels.h"
			break;
		}
		case  4:
		{
			#define SELECT_ROW  9
			#include "ClockOutPixels.h"
			break;
		}
		case  5:
		{
			#define SELECT_ROW  5
			#include "ClockOutPixels.h"
			break;
		}
		case  6:
		{
			#define SELECT_ROW  8
			#include "ClockOutPixels.h"
			break;
		}
		case  7:
		{
			#define SELECT_ROW  4
			#include "ClockOutPixels.h"
			break;
		}
		case  8:
		{
			#define SELECT_ROW 23
			#include "ClockOutPixels.h"
			break;
		}
		case  9:
		{
			#define SELECT_ROW  3
			#include "ClockOutPixels.h"
			break;
		}
		case 10:
		{
			#define SELECT_ROW 22
			#include "ClockOutPixels.h"
			break;
		}
		case 11:
		{
			#define SELECT_ROW  2
			#include "ClockOutPixels.h"
			break;
		}
		case 12:
		{
			#define SELECT_ROW 21
			#include "ClockOutPixels.h"
			break;
		}
		case 13:
		{
			#define SELECT_ROW  1
			#include "ClockOutPixels.h"
			break;
		}
		case 14:
		{
			#define SELECT_ROW 20
			#include "ClockOutPixels.h"
			break;
		}
		case 15:
		{
			#define SELECT_ROW  0
			#include "ClockOutPixels.h"
			break;
		}
		case 16:
		{
			#define SELECT_ROW 19
			#include "ClockOutPixels.h"
			break;
		}
		case 17:
		{
			#define SELECT_ROW 15
			#include "ClockOutPixels.h"
			break;
		}
		case 18:
		{
			#define SELECT_ROW 18
			#include "ClockOutPixels.h"
			break;
		}
		case 19:
		{
			#define SELECT_ROW 14
			#include "ClockOutPixels.h"
			break;
		}
		case 20:
		{
			#define SELECT_ROW 17
			#include "ClockOutPixels.h"
			break;
		}
		case 21:
		{
			#define SELECT_ROW 13
			#include "ClockOutPixels.h"
			break;
		}
		case 22:
		{
			#define SELECT_ROW 16
			#include "ClockOutPixels.h"
			break;
		}
		case 23:
		{
			#define SELECT_ROW 12
			#include "ClockOutPixels.h"
			break;
		}
	}
	PORTD |= (1 << PORTD6); // storage register clock high
	PORTD &= ~(1 << PORTD6); // storage register clock low
	
	// now do state machine book-keeping
	if(g_DisplayReg.Half-- == 0)
	{
		g_DisplayReg.Half = 1;
		
		if(g_DisplayReg.Y-- == 0)
		{
			g_DisplayReg.Y = BufferHeight - 1;
			
			if(--g_DisplayReg.BitPlaneHold == 0)
			{
				if(g_DisplayReg.BitPlane-- == 0)
				{
					g_DisplayReg.BitPlane = BufferBitPlanes - 1;
				
					LatchInFrameSwap();
					LatchInBrightness();
				
					g_DisplayReg.FrameChanged = true;
				}
				
				g_DisplayReg.BitPlaneHold = g_DisplayReg.GammaTable[g_DisplayReg.BitPlane];
			}
		}
	}
}

// Interrupt handler for timer to ensure that the display updates are regular and not delayed by any io or command processing
ISR(TIMER2_COMPA_vect, ISR_BLOCK)
{
	RefreshDisplay();
	TIFR2 |= OCF2A;
	TCNT2 = 0;
}
