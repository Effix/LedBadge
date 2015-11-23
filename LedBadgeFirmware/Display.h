#ifndef DISPLAY_H_
#define DISPLAY_H_

#include <avr/sfr_defs.h>
#include <avr/io.h>
#include <avr/interrupt.h>
#include <avr/cpufunc.h>
#include <avr/pgmspace.h>

enum
{
	BufferWidth = 48,											// pixels across
	BufferHeight = 12,											// pixels tall
	BufferBitPlanes = 3,										// unpacked bit-planes, 2 bits -> black + 3 gray levels
	BufferBitPlaneStride = BufferWidth / 8,						// bit-planes are 1bbp
	BufferBitPlaneLength = BufferBitPlaneStride * BufferHeight,	// full bit-plane size
	BufferLength = BufferBitPlaneLength * BufferBitPlanes,		// full unpacked frame buffer size
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
	const unsigned char *BufferP;								// points at the next 8 pixels to go out
	unsigned int Frame;											// current frame
	volatile bool FrameChanged;									// true if frame just changed
	bool BufferSelect;											// index of the current front buffer
	unsigned char BrightnessLevel;								// current output brightness
	unsigned char GammaTable[BufferBitPlanes];					// hold timings for the bit-planes
	unsigned char *FrontBuffer;									// current front buffer
	unsigned char *BackBuffer;									// current back buffer
	unsigned char Buffers[BufferCount][BufferLength];			// the storage for the front and back buffers
};

extern DisplayState g_DisplayReg;
extern const unsigned char g_RowDitherTable[BufferHeight];

// Sets a pixel value in a buffer
// Clips to the bounds of the buffer
inline void SetPix(unsigned char x, unsigned char y, unsigned char val, unsigned char *buffer = g_DisplayReg.BackBuffer);

// Reads a pixel value from a buffer
// Clips to the buffer bounds (returns 0 for out or bounds reads)
inline unsigned char GetPix(unsigned char x, unsigned char y, unsigned char *buffer = g_DisplayReg.BackBuffer);

// Set a block of pixels in a buffer to a particular value
void SolidFill(unsigned char x, unsigned char y, unsigned char width, unsigned char height, unsigned char color, unsigned char *buffer = g_DisplayReg.BackBuffer);

// Set a block of pixels in a buffer to the given data (read from the serial port, 2bpp packed format)
void Fill(unsigned char x, unsigned char y, unsigned char width, unsigned char height, unsigned char *buffer = g_DisplayReg.BackBuffer);

// Copy a block of pixels in a buffer to somewhere else
void Copy(unsigned char srcX, unsigned char srcY, unsigned char dstX, unsigned char dstY, unsigned char width, unsigned char height, unsigned char *srcBuffer, unsigned char *dstBuffer);

// Return a block of pixels from a buffer (sending it out to the serial port, 2bpp packed)
void ReadRect(unsigned char x, unsigned char  y, unsigned char width, unsigned char height, unsigned char *buffer = g_DisplayReg.BackBuffer);

// Set the image to show at startup (saves the front buffer to non-volatile memory)
void SetPowerOnImage();

// Flips the front and back buffers (latches over at the end of the frame)
void SwapBuffers();

// Sets the overall image brightness (latches over at the end of the frame)
void SetBrightness(unsigned char brightness);

// Set the hold values for the gray scale bit-planes
// Values are differential and the brightnesses are effectively a, a+b, and a+b+c 
// So, in order to get a 1, 5, 9 spread, you would pass in a=1, b=4, c=4
void SetHoldTimings(unsigned char a, unsigned char b, unsigned char c);

// Sets up the ports bound to the led drivers configures the output state, and fills the front buffer with the startup image
// Called once at program start
void ConfigureDisplay();

//
// Inline implementations
//

// Sets a pixel value in a buffer
// Clips to the bounds of the buffer
inline void SetPix(unsigned char x, unsigned char y, unsigned char val, unsigned char *buffer)
{
	if(x < BufferWidth && y < BufferHeight)
	{
		// figure out the packed bit position and mask for the bit-plane
		const unsigned char xBitPos = 7 - (x & 7);
		const unsigned char xPos = x >> 3;
		const unsigned char bit = 1 << xBitPos;
		const unsigned char mask = ~bit;
	
		unsigned char *p = &buffer[y * BufferBitPlaneStride + xPos];

		// expand the 0-3 pixel value into the 3 bit-planes
		*p = (*p & mask) | (val > 0 ? bit : 0);
		p += BufferBitPlaneLength;
		*p = (*p & mask) | (val > 1 ? bit : 0);
		p += BufferBitPlaneLength;
		*p = (*p & mask) | (val > 2 ? bit : 0);
	}
}

// Reads a pixel value from a buffer
// Clips to the buffer bounds (returns 0 for out or bounds reads)
inline unsigned char GetPix(unsigned char x, unsigned char y, unsigned char *buffer)
{
	if(x < BufferWidth && y < BufferHeight)
	{
		// figure out the packed bit position and mask for the bit-plane
		const unsigned char xBitPos = 7 - (x & 7);
		const unsigned char xPos = x >> 3;
		const unsigned char mask = (1 << xBitPos);
	
		unsigned char *p = &buffer[y * BufferBitPlaneStride + xPos];

		// un-expand the bit-planes by checking the most significant plane
		if(*(p + 2 * BufferBitPlaneLength) & mask)
			return 3;
		if(*(p + 1 * BufferBitPlaneLength) & mask)
			return 2;
		if(*p & mask)
			return 1;
		return 0;
	}
	else
	{
		return 0;
	}
}

#endif /* DISPLAY_H_ */