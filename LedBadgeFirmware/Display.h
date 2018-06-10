#ifndef DISPLAY_H_
#define DISPLAY_H_

#include <avr/sfr_defs.h>
#include <avr/io.h>
#include <avr/interrupt.h>
#include <avr/cpufunc.h>
#include <avr/pgmspace.h>

// a packed block of 8 2bpp pixels broken up into 2 bit planes
typedef unsigned int Pix2x8;

struct PixelFormat
{
	enum Enum
	{
		OneBit,
		TwoBits,
	};
};

struct FadingAction
{
	enum Enum
	{
		None,
		In,
		Out
	};
};

struct EndOfFadeAction
{
	enum Enum
	{
		None,
		Clear,
		RestartAnim,
		ResumeAnim
	};
};

typedef unsigned char (*FetchByte)(bool moreBytes);

enum
{
#if defined(__AVR_ATmega88PA__)
	BufferWidth = 48,											// pixels across
#elif defined(__AVR_ATmega8A__)
	BufferWidth = 36,											// pixels across
#endif
	BufferHeight = 12,											// pixels tall
	BufferPixels = BufferWidth * BufferHeight,					// total pixels
	BufferBitPlanes = 3,										// unpacked bit-planes, 2 bits -> black + 3 gray levels
	BufferBitPlaneStride = (BufferWidth + 7) / 8,				// bit-planes are 1bbp
	BufferBitPlaneLength = BufferBitPlaneStride * BufferHeight,	// full bit-plane size
	BufferLength = BufferBitPlaneLength * BufferBitPlanes,		// full unpacked frame buffer size
	BufferPackedLength = (BufferPixels + 3) / 4,				// full packed frame length (from a fill command)
	BufferCount = 2,											// buffers in the swap chain (front/back)
	
	BrightnessLevels = 256										// brightness look up table size
};

struct DisplayState
{
	volatile bool SwapRequest;									// true to swap the buffers at the end of the frame output
	volatile bool ChangeBrightnessRequest;						// true to update brightness at the end of the frame output
	unsigned char Y;											// current output row
	unsigned char Half;											// current side of the output row (scan lines are split in half)
	unsigned char BitPlane;										// currently displaying bit-plane index
	unsigned char BitPlaneHold;									// remaining count on current bit-plane
	unsigned char SoftwarePWMHold;								// remaining count for brightness control timing this cycle
	unsigned char SoftwarePWMPeriod;							// the count per cycle for brightness control timing
	const unsigned char *BufferP;								// points at the next 8 pixels to go out
	volatile bool FrameChanged;									// true if frame just changed
	volatile bool TimeoutAllowUpdate;							// true if timeout counter can change
	unsigned char TimeoutTrigger;								// idle frame count threshold
	unsigned char TimeoutCounter;								// idle frames so far...
	FadingAction::Enum FadeState;								// current action for the fade state machine
	unsigned char FadeCounter;									// counter for the fade state machine
	bool IdleFadeEnable;										// true to invoke fading to the idle reset image
	EndOfFadeAction::Enum IdleEndFadeAction;					// what happens before the badge fades back in
	bool BufferSelect;											// index of the current front buffer
	unsigned char BrightnessLevel;								// current output brightness
	unsigned char GammaTable[BufferBitPlanes];					// hold timings for the bit-planes
	unsigned char *FrontBuffer;									// current front buffer
	unsigned char *BackBuffer;									// current back buffer
};

extern DisplayState g_DisplayReg;
extern const unsigned char g_RowDitherTable[BufferHeight];

// Set a block of pixels in a buffer to a particular value
// The x and width parameters are in blocks, not pixels
void SolidFill(unsigned char x, unsigned char y, unsigned char width, unsigned char height, Pix2x8 val, unsigned char *buffer = g_DisplayReg.BackBuffer);

// Set a block of pixels in a buffer to the given data (read from the serial port)
// The x and width parameters are in blocks, not pixels
void Fill(unsigned char x, unsigned char y, unsigned char width, unsigned char height, PixelFormat::Enum format, FetchByte fetch, unsigned char *buffer = g_DisplayReg.BackBuffer);

// Copy a block of pixels in a buffer to somewhere else
// The x and width parameters are in blocks, not pixels
void Copy(unsigned char srcX, unsigned char srcY, unsigned char dstX, unsigned char dstY, unsigned char width, unsigned char height, unsigned char *srcBuffer, unsigned char *dstBuffer);

// Return a block of pixels from a buffer (sending it out to the serial port, 2bpp packed)
void ReadRect(unsigned char x, unsigned char y, unsigned char width, unsigned char height, PixelFormat::Enum format, unsigned char *buffer = g_DisplayReg.BackBuffer);

// Clears a buffer to black (faster than solid fill)
void ClearBuffer(unsigned char *buffer = g_DisplayReg.BackBuffer);

// Fast copy of a buffer
void CopyWholeBuffer(unsigned char *srcBuffer, unsigned char *dstBuffer);

// Flips the front and back buffers (latches over at the end of the frame)
void SwapBuffers();

// Sets the overall image brightness (latches over at the end of the frame)
void SetBrightness(unsigned char brightness);

// Gets the overall image brightness (it may not be latched over yet)
unsigned char GetBrightness();

// Set the hold values for the gray scale bit-planes
// Values are differential and the brightnesses are effectively a, a+b, and a+b+c 
// So, in order to get a 1, 5, 9 spread, you would pass in a=1, b=4, c=4
void SetHoldTimings(unsigned char a, unsigned char b, unsigned char c);

void GetHoldTimings(unsigned char *a, unsigned char *b, unsigned char *c);

// Sets the timeout parameters and behavior
// A timeout of 255 disables idle timeouts
void SetIdleTimeout(bool fade, EndOfFadeAction::Enum endFadeAction, unsigned char timeout);

void GetIdleTimeout(bool *fade, EndOfFadeAction::Enum *endFadeAction, unsigned char *timeout);

// Heartbeat to reset the idle timeout counter
void ResetIdleTime();

void SetFadeState(unsigned char counter, FadingAction::Enum action);

void GetFadeState(unsigned char *counter, FadingAction::Enum *action);

// Sets up the ports bound to the led drivers configures the output state, and fills the front buffer with the startup image
// Called once at program start
void ConfigureDisplay();

// Helper for blanking out the display during long operations while interrupts are disabled
void EnableDisplay(bool enable);

#endif /* DISPLAY_H_ */