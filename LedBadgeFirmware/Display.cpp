#include "Display.h"
#include "Serial.h"
#include "Eeprom.h"
#include "Commands.h"
#include <util/atomic.h>

template<bool pred> struct CT_Assert { typedef char arr[pred ? 0 : -1]; };
CT_Assert<sizeof(unsigned char) == 1> AssertSizeOfChar;
CT_Assert<sizeof(Pix2x8) == 2> AssertSizeOfPix2x8;

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
#if defined(__AVR_ATmega88PA__)
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
#elif defined(__AVR_ATmega8A__)
	0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 
	0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 
	0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 
	0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 
	0xFF, 0xFF, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 
	0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 
	0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0D, 0x0D, 0x0D, 0x0D, 0x0D, 0x0D, 0x0D, 0x0D, 0x0D, 0x0D, 
	0x0D, 0x0D, 0x0D, 0x0D, 0x0D, 0x0D, 0x0D, 0x0D, 0x0D, 0x0D, 0x0D, 0x0D, 0x0D, 0x0C, 0x0C, 0x0C, 
	0x0C, 0x0C, 0x0C, 0x0C, 0x0C, 0x0C, 0x0C, 0x0C, 0x0C, 0x0C, 0x0C, 0x0C, 0x0C, 0x0C, 0x0C, 0x0B, 
	0x0B, 0x0B, 0x0B, 0x0B, 0x0B, 0x0B, 0x0B, 0x0B, 0x0B, 0x0B, 0x0B, 0x0B, 0x0B, 0x0B, 0x0A, 0x0A, 
	0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x09, 0x09, 0x09, 0x09, 0x09, 
	0x09, 0x09, 0x09, 0x09, 0x09, 0x09, 0x09, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 
	0x08, 0x08, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x06, 0x06, 0x06, 0x06, 
	0x06, 0x06, 0x06, 0x06, 0x06, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x04, 0x04, 
	0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x02, 0x02, 
	0x02, 0x02, 0x02, 0x02, 0x02, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00
#endif
};

static unsigned char g_Buffer0[BufferLength] __attribute__ ((section (".buffer0")));
static unsigned char g_Buffer1[BufferLength] __attribute__ ((section (".buffer1")));

// Display state machine values
DisplayState g_DisplayReg = {};

// Clamps a span along an axis to the bounds [0, Extent)
template<unsigned char Extent> void Clamp(unsigned char &pos, unsigned char &size)
{
	if(pos >= Extent)
	{
		size = 0;
		pos = Extent - 1;
	}
	
	if(pos + size > Extent)
	{
		unsigned char delta = (pos + size) - Extent;
		if(delta > size)
		{
			size = 0;
		}
		else
		{
			size -= delta;
		}
	}
}

// Sets a block of 8 pixel values in a buffer
void SetPixBlockUnsafe(unsigned char *buffer, Pix2x8 val)
{
	const unsigned char low = val & 0xFF;
	const unsigned char high = (val >> 8) & 0xFF;

	*buffer = low | high;
	buffer += BufferBitPlaneLength;
	*buffer = high;
	buffer += BufferBitPlaneLength;
	*buffer = low & high;
}

// Sets a block of 8 pixel values in a buffer
// The x parameter is in blocks, not pixels
// Clips to the bounds of the buffer
void SetPixBlock(unsigned char x, unsigned char y, Pix2x8 val, unsigned char *buffer)
{
	if(x < BufferBitPlaneStride && y < BufferHeight)
	{
		SetPixBlockUnsafe(buffer + y * BufferBitPlaneStride + x, val);
	}
}

// Reads a block of 8 pixel values from a buffer
Pix2x8 GetPixBlockUnsafe(unsigned char *buffer)
{
	const unsigned char b0 = *buffer;
	buffer += BufferBitPlaneLength;
	const unsigned char b1 = *buffer;
	buffer += BufferBitPlaneLength;
	const unsigned char b2 = *buffer;

	const unsigned char high = b1;
	const unsigned char low = (b0 ^ b1) | b2;

	return (high << 8) | low;
}

// Reads a block of 8 pixel values from a buffer
// The x parameter is in blocks, not pixels
// Clips to the buffer bounds (returns 0 for out or bounds reads)
Pix2x8 GetPixBlock(unsigned char x, unsigned char y, unsigned char *buffer)
{
	if(x < BufferBitPlaneStride && y < BufferHeight)
	{
		return GetPixBlockUnsafe(buffer);
	}
	else
	{
		return 0;
	}
}

// Set a block of pixels in a buffer to a particular value
// The x and width parameters are in blocks, not pixels
void SolidFill(unsigned char x, unsigned char y, unsigned char width, unsigned char height, Pix2x8 val, unsigned char *buffer)
{
	Clamp<BufferBitPlaneStride>(x, width);
	Clamp<BufferHeight>(y, height);
	
	unsigned char *b0 = buffer + y * BufferBitPlaneStride + x;
	for(unsigned char iy = height; iy; --iy, b0 += BufferBitPlaneStride)
	{
		buffer = b0;
		for(unsigned char ix = width; ix; --ix, ++buffer)
		{
			SetPixBlockUnsafe(buffer, val);
		}
	}
}

// Set a block of pixels in a buffer to the given data (read from the serial port)
// The x and width parameters are in blocks, not pixels
void Fill(unsigned char x, unsigned char y, unsigned char width, unsigned char height, PixelFormat::Enum format, FetchByte fetch, unsigned char *buffer)
{
	unsigned int count = width * height;
	for(unsigned char iy = y, sy = y + height; iy < sy; ++iy)
	{
		for(unsigned char ix = x, sx = x + width; ix < sx; ++ix)
		{
			Pix2x8 data;
			if(format == PixelFormat::OneBit)
			{
				data = fetch(--count > 0);
				data = (data << 8) | data;
			}
			else
			{
				data = fetch(true);
				data = (data << 8) | fetch(--count > 0);
			}
			
			SetPixBlock(ix, iy, data, buffer);
		}
	}
}

// Copy a block of pixels in a buffer to somewhere else
// The x and width parameters are in blocks, not pixels
void Copy(unsigned char srcX, unsigned char srcY, unsigned char dstX, unsigned char dstY, unsigned char width, unsigned char height, unsigned char *srcBuffer, unsigned char *dstBuffer)
{
	Clamp<BufferBitPlaneStride>(srcX, width);
	Clamp<BufferHeight>(srcY, height);
	Clamp<BufferBitPlaneStride>(dstX, width);
	Clamp<BufferHeight>(dstY, height);
	
	unsigned char *bs = srcBuffer + srcY * BufferBitPlaneStride + srcX;
	unsigned char *bd = dstBuffer + dstY * BufferBitPlaneStride + dstX;
	for(unsigned char iy = height; iy; --iy, bs += BufferBitPlaneStride, bd += BufferBitPlaneStride)
	{
		srcBuffer = bs;
		dstBuffer = bd;
		for(unsigned char ix = width; ix; --ix, ++srcBuffer, ++dstBuffer)
		{
			SetPixBlockUnsafe(dstBuffer, GetPixBlockUnsafe(srcBuffer));
		}
	}
}

// Return a block of pixels from a buffer (sending it out to the serial port, 2bpp packed)
void ReadRect(unsigned char x, unsigned char y, unsigned char width, unsigned char height, PixelFormat::Enum format, unsigned char *buffer)
{
	for(unsigned char iy = y, sy = y + height; iy < sy; ++iy)
	{
		for(unsigned char ix = x, sx = x + width; ix < sx; ++ix)
		{
			Pix2x8 val = GetPixBlock(ix, iy, buffer);
			WriteSerialData((val >> 8) & 0xFF);
			if(format == PixelFormat::TwoBits)
			{
				WriteSerialData(val & 0xFF);
			}
		}
	}
}

// Clears a buffer to black (faster than solid fill)
void ClearBuffer(unsigned char *buffer)
{
	for(unsigned char i = 0; i < BufferLength; ++i)
	{
		*buffer++ = 0;
	}
}

// Fast copy of a buffer
void CopyWholeBuffer(unsigned char *srcBuffer, unsigned char *dstBuffer)
{
	for(unsigned char i = 0; i < BufferLength; ++i)
	{
		*dstBuffer++ = *srcBuffer++;
	}
}

// Flips the front and back buffers (latches over at the end of the frame)
void SwapBuffers()
{
	g_DisplayReg.SwapRequest = true;
	while(g_DisplayReg.SwapRequest)
	{
		PumpAck();
	}
}

// Commits the requested buffer swap at the end of the frame
void LatchInFrameSwap()
{
	if(g_DisplayReg.SwapRequest)
	{
		g_DisplayReg.SwapRequest = false;
		
		g_DisplayReg.BufferSelect = !g_DisplayReg.BufferSelect;
		g_DisplayReg.FrontBuffer = g_DisplayReg.BufferSelect == 0 ? g_Buffer0 : g_Buffer1;
		g_DisplayReg.BackBuffer = g_DisplayReg.BufferSelect == 0 ? g_Buffer1 : g_Buffer0;
	}
}

// Sets the overall image brightness (latches over at the end of the frame)
void SetBrightness(unsigned char brightness)
{
	g_DisplayReg.BrightnessLevel = brightness;
	g_DisplayReg.ChangeBrightnessRequest = true;
}

// Heartbeat to reset the idle timeout counter
void ResetIdleTime()
{
	g_DisplayReg.TimeoutCounter = 0;
	g_DisplayReg.TimeoutAllowUpdate = true;
}

// Stops and clears any in progress fade
void ResetFade()
{
	g_DisplayReg.TimeoutCounter = 0;
	g_DisplayReg.FadeCounter = 0;
	g_DisplayReg.FadeState = FadingAction::None;
}

// Begins a fade sequence
void StartFade()
{
	g_DisplayReg.FadeState = FadingAction::Out;
	g_DisplayReg.FadeCounter = g_DisplayReg.BrightnessLevel;
}

// Modifies the overall display brightness
void SetBrightnessLevelRegisters(unsigned char level)
{
	// the OE (output enable) signal is wired up to one of the pwm pins, so this has way more intensity levels than we can generate with the gray scale bit-planes
	// since it is a separate pin, it overlays nicely, but dim values can end up looking a little flickery
#if defined(__AVR_ATmega88PA__)
	OCR0B = pgm_read_byte(&g_BrightnessTable[level]);
#elif defined(__AVR_ATmega8A__)
	g_DisplayReg.SoftwarePWMPeriod = pgm_read_byte(&g_BrightnessTable[level]);
#endif
}

// Commits the requested brightness change when it is safe to do so
void LatchInBrightness()
{
	if(g_DisplayReg.ChangeBrightnessRequest)
	{
		g_DisplayReg.ChangeBrightnessRequest = false;
		SetBrightnessLevelRegisters(g_DisplayReg.BrightnessLevel);
		ResetFade();
	}
}

void RunEndOfFadeAction()
{
	switch(g_DisplayReg.IdleEndFadeAction)
	{
		case EndOfFadeAction::ResumeAnim:
		{
			g_CommandReg.AnimReadPosition = g_CommandReg.AnimBookmark;
			g_CommandReg.AnimPlaying = AnimState::Playing;
			break;
		}
		case EndOfFadeAction::RestartAnim:
		{
			g_CommandReg.AnimReadPosition = g_CommandReg.AnimStart;
			g_CommandReg.AnimPlaying = AnimState::Playing;
			break;
		}
		case EndOfFadeAction::Clear:
		{
			ClearBuffer(g_DisplayReg.FrontBuffer);
			break;
		}
		case  EndOfFadeAction::None: break;
	}
}

// Updates the timeout state machine
void PumpTimeout()
{
	if(g_DisplayReg.TimeoutTrigger < 255 && g_DisplayReg.FadeState == FadingAction::None && g_DisplayReg.TimeoutAllowUpdate && !g_CommandReg.AnimPlaying)
	{
		if(g_DisplayReg.TimeoutCounter >= g_DisplayReg.TimeoutTrigger)
		{
			if(g_DisplayReg.IdleFadeEnable)
			{
				StartFade();
			}
			else
			{
				RunEndOfFadeAction();
				g_DisplayReg.TimeoutCounter = 0;
			}
			
			g_DisplayReg.TimeoutAllowUpdate = false;
		}
		else
		{
			++g_DisplayReg.TimeoutCounter;
		}
	}
}

// Updates the fade state machine
void PumpFade()
{
	switch(g_DisplayReg.FadeState)
	{
		case FadingAction::In:
		{
			if(g_DisplayReg.FadeCounter >= g_DisplayReg.BrightnessLevel)
			{
				g_DisplayReg.TimeoutCounter = 0;
				g_DisplayReg.FadeState = FadingAction::None;
			}
			else
			{
				++g_DisplayReg.FadeCounter;
			}
			SetBrightnessLevelRegisters(g_DisplayReg.FadeCounter);
			break;
		}
		case FadingAction::Out:
		{
			if(g_DisplayReg.FadeCounter == 0)
			{
				RunEndOfFadeAction();
				g_DisplayReg.FadeState = FadingAction::In;
			}
			else
			{
				--g_DisplayReg.FadeCounter;
			}
			SetBrightnessLevelRegisters(g_DisplayReg.FadeCounter);
			break;
		}
		case FadingAction::None: break;
	}
}

// Sets up the ports bound to the led drivers configures the output state, and fills the front buffer with the startup image
// Called once at program start
void ConfigureDisplay()
{
#if defined(__AVR_ATmega88PA__)
	// data and clock pins
	DDRB |= (1 << DDB1) | (1 << DDB0);
	PORTB &= ~((1 << PORTB1) | (1 << PORTB0));
	DDRD |= (1 << DDD7) | (1 << DDD6) | (1 << DDD5);
	PORTD &= ~((1 << PORTD7) | (1 << PORTD6) | (1 << PORTD5));
	
	// brightness pwm timer
	TCCR0A |= (1 << WGM00) | (1 << WGM01) | (1 << COM0B0) | (1 << COM0B1);
	TCCR0B |= (1 << CS00);
	SetBrightnessLevelRegisters(0);
	
	// refresh timer
	TCCR2B |= (1 << CS21);
	OCR2A = 336 / 8; // ~186hz @ 12mhz
	TIMSK2 |= (1 << OCIE2A);
#elif defined(__AVR_ATmega8A__)
	// data and clock pins
	DDRB |= (1 << DDB0) | (1 << DDB1) | (1 << DDB2) | (1 << DDB5) | (1 << DDB6) | (1 << DDB7);
	PORTB &= ~((1 << PORTB0) | (1 << PORTB1) | (1 << PORTB2) | (1 << PORTB5) | (1 << PORTB6) | (1 << PORTB7));
	DDRC |= (1 << DDC0) | (1 << DDC1) | (1 << DDC2) | (1 << DDC3);
	PORTC &= ~((1 << PORTC0) | (1 << PORTC1) | (1 << PORTC2) | (1 << PORTC3));
	DDRD |= (1 << DDD3) | (1 << DDD4) | (1 << DDD5) | (1 << DDD6) | (1 << DDD7);
	PORTD &= ~((1 << PORTD3) | (1 << PORTD4) | (1 << PORTD5) | (1 << PORTD6) | (1 << PORTD7));
	
	// disable output
	PORTB |= (1 << PORTB5);
	
	// refresh timer
	OCR2 = 352 / 8; // ~236hz @ 8mhz
	TCCR2 |= (1 << CS21);
	TIMSK |= (1 << OCIE2);
#endif

	// omitted fields are 0 initialized
	g_DisplayReg.FrontBuffer = g_Buffer0;
	g_DisplayReg.BackBuffer = g_Buffer1;
	g_DisplayReg.BrightnessLevel = BrightnessLevels / 2;
	g_DisplayReg.GammaTable[0] = 1;
	g_DisplayReg.GammaTable[1] = 3;
	g_DisplayReg.GammaTable[2] = 4;
	g_DisplayReg.Y = BufferHeight - 1;
	g_DisplayReg.Half = 1;
	g_DisplayReg.BitPlane = BufferBitPlanes - 1;
	g_DisplayReg.BitPlaneHold = g_DisplayReg.GammaTable[g_DisplayReg.BitPlane];
	g_DisplayReg.BufferP = g_DisplayReg.FrontBuffer + BufferLength;
	g_DisplayReg.TimeoutTrigger = 255;
	g_DisplayReg.FadeState = FadingAction::In;
}

// Helper for blanking out the display during long operations while interrupts are disabled
void EnableDisplay(bool enable)
{
	if(enable)
	{
		SetBrightnessLevelRegisters(g_DisplayReg.FadeState != FadingAction::None ? g_DisplayReg.FadeCounter : g_DisplayReg.BrightnessLevel);
	}
	else
	{
		SetBrightnessLevelRegisters(0);
	}
}

// Updates one segment of the display (one half a a row)
void RefreshDisplay()
{
	unsigned char y = g_RowDitherTable[g_DisplayReg.Y];
	g_DisplayReg.BufferP = g_DisplayReg.FrontBuffer + 
		(g_DisplayReg.BitPlane * BufferBitPlaneLength) + 
		(y * BufferBitPlaneStride) + 
		(g_DisplayReg.Half == 0 ? BufferBitPlaneStride / 2 : BufferBitPlaneStride);

#if defined(__AVR_ATmega88PA__)

	unsigned char row = (y << 1) + g_DisplayReg.Half;
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

#elif defined(__AVR_ATmega8A__)
	
	// disable output
	PORTB |= (1 << PORTB5);
	
	/*if(g_DisplayReg.SoftwarePWMHold != 0)
	{
		if(g_DisplayReg.SoftwarePWMHold != 0xFF)
		{
			--g_DisplayReg.SoftwarePWMHold;
		}
		
		// early out of the state machine
		return;
	}*/
	
	unsigned char portB = PORTB;
	unsigned char portD = PORTD;
	
	switch(y)
	{
		case  0:
		{
			#define SELECT_ROW 0
			#include "ClockOutPixels.h"
			break;
		}
		case  1:
		{
			#define SELECT_ROW 1
			#include "ClockOutPixels.h"
			break;
		}
		case  2:
		{
			#define SELECT_ROW 2
			#include "ClockOutPixels.h"
			break;
		}
		case  3:
		{
			#define SELECT_ROW 3
			#include "ClockOutPixels.h"
			break;
		}
		case  4:
		{
			#define SELECT_ROW 4
			#include "ClockOutPixels.h"
			break;
		}
		case  5:
		{
			#define SELECT_ROW 5
			#include "ClockOutPixels.h"
			break;
		}
		case  6:
		{
			#define SELECT_ROW 6
			#include "ClockOutPixels.h"
			break;
		}
		case  7:
		{
			#define SELECT_ROW 7
			#include "ClockOutPixels.h"
			break;
		}
		case  8:
		{
			#define SELECT_ROW 8
			#include "ClockOutPixels.h"
			break;
		}
		case  9:
		{
			#define SELECT_ROW 9
			#include "ClockOutPixels.h"
			break;
		}
		case 10:
		{
			#define SELECT_ROW 10
			#include "ClockOutPixels.h"
			break;
		}
		case 11:
		{
			#define SELECT_ROW 11
			#include "ClockOutPixels.h"
			break;
		}
	}
	
	// enable output
	PORTB &= ~(1 << PORTB5);
	
#endif
	
	// now do state machine book-keeping
#if defined(__AVR_ATmega88PA__)
	if(g_DisplayReg.Half-- == 0)
#endif
	{
#if defined(__AVR_ATmega88PA__)
		g_DisplayReg.Half = 1;
#endif
		if(g_DisplayReg.Y-- == 0)
		{
			g_DisplayReg.Y = BufferHeight - 1;
			
#if defined(__AVR_ATmega8A__)
			g_DisplayReg.SoftwarePWMHold = g_DisplayReg.SoftwarePWMPeriod;
#endif
			
			if(--g_DisplayReg.BitPlaneHold == 0)
			{
				if(g_DisplayReg.BitPlane-- == 0)
				{
					g_DisplayReg.BitPlane = BufferBitPlanes - 1;
				
					LatchInFrameSwap();
					LatchInBrightness();
					PumpTimeout();
					PumpFade();
				
					g_DisplayReg.FrameChanged = true;
				}
				
				g_DisplayReg.BitPlaneHold = g_DisplayReg.GammaTable[g_DisplayReg.BitPlane];
			}
		}
	}
}

// Interrupt handler for timer to ensure that the display updates are regular and not delayed by any io or command processing
#if defined(__AVR_ATmega88PA__)
ISR(TIMER2_COMPA_vect, ISR_BLOCK)
#elif defined(__AVR_ATmega8A__)
ISR(TIMER2_COMP_vect, ISR_BLOCK)
#endif
{
	RefreshDisplay();
	TCNT2 = 0;
}
