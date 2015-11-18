#ifndef DISPLAY_H_
#define DISPLAY_H_

#include <avr/sfr_defs.h>
#include <avr/io.h>
#include <avr/interrupt.h>
#include <avr/cpufunc.h>
#include <avr/pgmspace.h>

enum
{
	BufferWidth = 48,
	BufferHeight = 12,
	BufferBitPlanes = 3,
	BufferBitPlaneStride = BufferWidth / 8,
	BufferBitPlaneLength = BufferBitPlaneStride * BufferHeight,
	BufferLength = BufferBitPlaneLength * BufferBitPlanes,
	BufferCount = 2,
	
	BrightnessLevels = 255
};

struct DisplayState
{
	volatile bool SwapRequest;
	volatile bool ChangeBrightnessRequest;
	unsigned char Y;
	unsigned char Half;
	unsigned char BitPlane;
	unsigned char BitPlaneHold;
	const unsigned char *BufferP;
	unsigned int Frame;
	volatile bool FrameChanged;
	bool BufferSelect;
	unsigned char BrightnessLevel;
	unsigned char GammaTable[BufferBitPlanes];
	unsigned char *FrontBuffer;
	unsigned char *BackBuffer;
	unsigned char Buffers[BufferCount][BufferLength];
};

extern DisplayState g_DisplayReg;
extern const unsigned char g_RowDitherTable[BufferHeight];

inline void SetPix(unsigned char x, unsigned char y, unsigned char val, unsigned char *buffer = g_DisplayReg.BackBuffer);
inline unsigned char GetPix(unsigned char x, unsigned char y, unsigned char *buffer = g_DisplayReg.BackBuffer);
void SolidFill(unsigned char x, unsigned char y, unsigned char width, unsigned char height, unsigned char color, unsigned char *buffer = g_DisplayReg.BackBuffer);
void Fill(unsigned char x, unsigned char y, unsigned char width, unsigned char height, unsigned char *buffer = g_DisplayReg.BackBuffer);
void Copy(unsigned char srcX, unsigned char srcY, unsigned char dstX, unsigned char dstY, unsigned char width, unsigned char height, unsigned char *srcBuffer, unsigned char *dstBuffer);
void ReadRect(unsigned char x, unsigned char  y, unsigned char width, unsigned char height, unsigned char *buffer = g_DisplayReg.BackBuffer);
void SetPowerOnImage();

void SwapBuffers();
void SetBrightness(unsigned char brightness);
void SetHoldTimings(unsigned char a, unsigned char b, unsigned char c);
void ConfigureDisplay();

//
//
//

inline void SetPix(unsigned char x, unsigned char y, unsigned char val, unsigned char *buffer)
{
	if(x < BufferWidth && y < BufferHeight)
	{
		const unsigned char xBitPos = 7 - (x & 7);
		const unsigned char xPos = x >> 3;
		const unsigned char bit = 1 << xBitPos;
		const unsigned char mask = ~bit;
	
		unsigned char *p = &buffer[y * BufferBitPlaneStride + xPos];

		*p = (*p & mask) | (val > 0 ? bit : 0);
		p += BufferBitPlaneLength;
		*p = (*p & mask) | (val > 1 ? bit : 0);
		p += BufferBitPlaneLength;
		*p = (*p & mask) | (val > 2 ? bit : 0);
	}
}

inline unsigned char GetPix(unsigned char x, unsigned char y, unsigned char *buffer)
{
	if(x < BufferWidth && y < BufferHeight)
	{
		const unsigned char xBitPos = 7 - (x & 7);
		const unsigned char xPos = x >> 3;
		const unsigned char mask = (1 << xBitPos);
	
		unsigned char *p = &buffer[y * BufferBitPlaneStride + xPos];

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