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
	BufferPixels = BufferWidth * BufferHeight,					// total pixels
	BufferBitPlanes = 3,										// unpacked bit-planes, 2 bits -> black + 3 gray levels
	BufferBitPlaneStride = BufferWidth / 8,						// bit-planes are 1bbp
	BufferBitPlaneLength = BufferBitPlaneStride * BufferHeight,	// full bit-plane size
	BufferLength = BufferBitPlaneLength * BufferBitPlanes,		// full unpacked frame buffer size
	BufferPackedLength = BufferPixels / 4,						// full packed frame length (from a fill command)
	BufferCount = 2,											// buffers in the swap chain (front/back)
	
	BrightnessLevels = 256										// brightness look up table size
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

struct DisplayState
{
	volatile bool SwapRequest;									// true to swap the buffers at the end of the frame output
	volatile bool ChangeBrightnessRequest;						// true to update brightness at the end of the frame output
	unsigned char Y;											// current output row
	unsigned char Half;											// current side of the output row (scan lines are split in half)
	unsigned char BitPlane;										// currently displaying bit-plane index
	unsigned char BitPlaneHold;									// remaining count on current bit-plane
	const unsigned char *BufferP;								// points at the next 8 pixels to go out
	volatile bool FrameChanged;									// true if frame just changed
	unsigned char TimeoutTrigger;								// idle frame count threshold
	unsigned char TimeoutCounter;								// idle frames so far...
	unsigned char FadeState;									// current action for the fade state machine
	unsigned char FadeCounter;									// counter for the fade state machine
	bool IdleFadeEnable;										// true to invoke fading to the idle reset image
	bool IdleResetToBootImage;									// true to reset to the startup image instead of just black
	bool BufferSelect;											// index of the current front buffer
	unsigned char BrightnessLevel;								// current output brightness
	unsigned char GammaTable[BufferBitPlanes];					// hold timings for the bit-planes
	unsigned char *FrontBuffer;									// current front buffer
	unsigned char *BackBuffer;									// current back buffer
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
void ReadRect(unsigned char x, unsigned char y, unsigned char width, unsigned char height, unsigned char *buffer = g_DisplayReg.BackBuffer);

// Clears a buffer to black (faster than solid fill)
void ClearBuffer(unsigned char *buffer = g_DisplayReg.BackBuffer);

// Fast copy of a buffer
void CopyWholeBuffer(unsigned char *srcBuffer, unsigned char *dstBuffer);

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

// Sets the timeout parameters and behavior
// A timeout of 255 disables idle timeouts
void SetIdleTimeout(unsigned char fade, unsigned char resetToBootImage, unsigned char timeout);

// Heartbeat to reset the idle timeout counter
void ResetIdleTime();

// Sets up the ports bound to the led drivers configures the output state, and fills the front buffer with the startup image
// Called once at program start
void ConfigureDisplay();

// Helper for blanking out the display during long operations while interrupts are disabled
void EnableDisplay(bool enable);

//
// Inline implementations
//

extern "C" unsigned char c_PixMasks[8];

inline void SetPixUnsafe(unsigned char x, unsigned char y, unsigned char val, unsigned char *buffer) __attribute__ ((always_inline));
inline unsigned char GetPixUnsafe(unsigned char x, unsigned char y, unsigned char *buffer) __attribute__ ((always_inline));

// Sets a pixel value in a buffer
inline void SetPixUnsafe(unsigned char x, unsigned char y, unsigned char val, unsigned char *buffer)
{
	/*
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
	*/
	
	asm volatile
	(
		"ldi     r23, %[stride]"				"\n\t"  // build y offset
		"mul     %[y], r23"						"\n\t"  // 
		"add     %[buffer], r0"					"\n\t"  // add y offset to the buffer pointer
		
		"ldi     r27, 0"						"\n\t"  // initialize the pointer to the mask bit with the 
		"mov     r26, %[x]"						"\n\t"  //   bottom 3 bits of the x location as the offset
		"andi    r26, 7"						"\n\t"  // 
		"subi    r26, lo8(-(c_PixMasks))"		"\n\t"  // add the start of the mask list to the offset
		"sbci    r27, hi8(-(c_PixMasks))"		"\n\t"  // 
		"ld      r23, x"						"\n\t"  // load mask bit
		
		"lsr     %[x]"							"\n\t"  // build x offset
		"lsr     %[x]"							"\n\t"  //
		"lsr     %[x]"							"\n\t"  // 
		"add     %[buffer], %[x]"				"\n\t"  // add y offset to the buffer pointer

		"cpi     %[val], 3"						"\n\t"  // jump to fill pattern
		"breq    .L_VAL_3_%="					"\n\t"  // 
		"cpi     %[val], 2"						"\n\t"  // 
		"breq    .L_VAL_2_%="					"\n\t"  // 
		"cpi     %[val], 1"						"\n\t"  // 
		"breq    .L_VAL_1_%="					"\n\t"  // 

		".L_VAL_0_%=:"							"\n\t"  // pattern for val == 0
		"com     r23"							"\n\t"  // invert bit into a mask
		"ld	     __tmp_reg__, %a[buffer]"		"\n\t"  // clear plane bit
		"and     __tmp_reg__, r23"				"\n\t"  // 
		"st      %a[buffer], __tmp_reg__"		"\n\t"  // 
		"subi    %[buffer], -%[length]"			"\n\t"  // offset to next bit plane
		"ld	     __tmp_reg__, %a[buffer]"		"\n\t"  // clear plane bit
		"and     __tmp_reg__, r23"				"\n\t"  // 
		"st      %a[buffer], __tmp_reg__"		"\n\t"  // 
		"subi    %[buffer], -%[length]"			"\n\t"  // offset to next bit plane
		"ld	     __tmp_reg__, %a[buffer]"		"\n\t"  // clear plane bit
		"and     __tmp_reg__, r23"				"\n\t"  // 
		"st      %a[buffer], __tmp_reg__"		"\n\t"  // 
		"rjmp    .L_END_%="						"\n\t"  // end function
		
		".L_VAL_1_%=:"							"\n\t"  // pattern for val == 1
		"ld	     __tmp_reg__, %a[buffer]"		"\n\t"  // set plane bit
		"or      __tmp_reg__, r23"				"\n\t"  // 
		"st      %a[buffer], __tmp_reg__"		"\n\t"  // 
		"subi    %[buffer], -%[length]"			"\n\t"  // offset to next bit plane
		"com     r23"							"\n\t"  // invert bit into a mask
		"ld	     __tmp_reg__, %a[buffer]"		"\n\t"  // clear plane bit
		"and     __tmp_reg__, r23"				"\n\t"  // 
		"st      %a[buffer], __tmp_reg__"		"\n\t"  // 
		"subi    %[buffer], -%[length]"			"\n\t"  // offset to next bit plane
		"ld	     __tmp_reg__, %a[buffer]"		"\n\t"  // clear plane bit
		"and     __tmp_reg__, r23"				"\n\t"  // 
		"st      %a[buffer], __tmp_reg__"		"\n\t"  // 
		"rjmp    .L_END_%="						"\n\t"  // end function
		
		".L_VAL_2_%=:"							"\n\t"  // pattern for val == 2
		"ld	     __tmp_reg__, %a[buffer]"		"\n\t"  // set plane bit
		"or      __tmp_reg__, r23"				"\n\t"  // 
		"st      %a[buffer], __tmp_reg__"		"\n\t"  // 
		"subi    %[buffer], -%[length]"			"\n\t"  // offset to next bit plane
		"ld	     __tmp_reg__, %a[buffer]"		"\n\t"  // set plane bit
		"or      __tmp_reg__, r23"				"\n\t"  // 
		"st      %a[buffer], __tmp_reg__"		"\n\t"  // 
		"subi    %[buffer], -%[length]"			"\n\t"  // offset to next bit plane
		"com     r23"							"\n\t"  // invert bit into a mask
		"ld	     __tmp_reg__, %a[buffer]"		"\n\t"  // clear plane bit
		"and     __tmp_reg__, r23"				"\n\t"  // 
		"st      %a[buffer], __tmp_reg__"		"\n\t"  // 
		"rjmp    .L_END_%="						"\n\t"  // end function
		
		".L_VAL_3_%=:"							"\n\t"  // pattern for val == 3
		"ld	     __tmp_reg__, %a[buffer]"		"\n\t"  // set plane bit
		"or      __tmp_reg__, r23"				"\n\t"  // 
		"st      %a[buffer], __tmp_reg__"		"\n\t"  // 
		"subi    %[buffer], -%[length]"			"\n\t"  // offset to next bit plane
		"ld	     __tmp_reg__, %a[buffer]"		"\n\t"  // set plane bit
		"or      __tmp_reg__, r23"				"\n\t"  // 
		"st      %a[buffer], __tmp_reg__"		"\n\t"  // 
		"subi    %[buffer], -%[length]"			"\n\t"  // offset to next bit plane
		"ld	     __tmp_reg__, %a[buffer]"		"\n\t"  // set plane bit
		"or      __tmp_reg__, r23"				"\n\t"  // 
		"st      %a[buffer], __tmp_reg__"		"\n\t"  // 
		
		".L_END_%=:"							"\n\t"
		
		:   "+r" (x),
			"+r" (buffer)
		:	[x] "r" (x),
			[y] "r" (y),
			[val] "r" (val),
			[buffer] "e" (buffer),
			[stride] "M" (BufferBitPlaneStride),
			[length] "M" (BufferBitPlaneLength)
		:	"r23", "r26", "r27"
	);
}

// Sets a pixel value in a buffer
// Clips to the bounds of the buffer
inline void SetPix(unsigned char x, unsigned char y, unsigned char val, unsigned char *buffer)
{
	if(x < BufferWidth && y < BufferHeight)
	{
		SetPixUnsafe(x, y, val, buffer);
	}
}

// Reads a pixel value from a buffer
inline unsigned char GetPixUnsafe(unsigned char x, unsigned char y, unsigned char *buffer)
{
	/*
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
	*/
	
	unsigned char result = 0;
	asm volatile
	(
		"ldi     r23, %[stride]"				"\n\t"  // build y offset
		"mul     %[y], r23"						"\n\t"  // 
		"add     %[buffer], r0"					"\n\t"  // add y offset to the buffer pointer
		
		"ldi     r27, 0"						"\n\t"  // initialize the pointer to the mask bit with the 
		"mov     r26, %[x]"						"\n\t"  //   bottom 3 bits of the x location as the offset
		"andi    r26, 7"						"\n\t"  // 
		"subi    r26, lo8(-(c_PixMasks))"		"\n\t"  // add the start of the mask list to the offset
		"sbci    r27, hi8(-(c_PixMasks))"		"\n\t"  // 
		"ld      r23, x"						"\n\t"  // load mask bit
		
		"lsr     %[x]"							"\n\t"  // build x offset
		"lsr     %[x]"							"\n\t"  //
		"lsr     %[x]"							"\n\t"  // 
		"add     %[buffer], %[x]"				"\n\t"  // add y offset to the buffer pointer
		
		"ld	     __tmp_reg__, %a[buffer]"		"\n\t"  // 
		"and     __tmp_reg__, r23"				"\n\t"  // 
		"breq    .L_P2_%="						"\n\t"  // 
		"inc     %[result]"						"\n\t"  // 
		
		".L_P2_%=:"								"\n\t"  // 
		"subi    %[buffer], -%[length]"			"\n\t"  // 
		"ld	     __tmp_reg__, %a[buffer]"		"\n\t"  // 
		"and     __tmp_reg__, r23"				"\n\t"  // 
		"breq    .L_P3_%="						"\n\t"  // 
		"inc     %[result]"						"\n\t"  // 
		
		".L_P3_%=:"								"\n\t"  // 
		"subi    %[buffer], -%[length]"			"\n\t"  // 
		"ld	     __tmp_reg__, %a[buffer]"		"\n\t"  // 
		"and     __tmp_reg__, r23"				"\n\t"  // 
		"breq    .L_END_%="						"\n\t"  // 
		"inc     %[result]"						"\n\t"  // 
		
		".L_END_%=:"							"\n\t"  // 
		
		:   "+r" (x),
			"+r" (buffer),
			"=r" (result)
		:	[x] "r" (x),
			[y] "r" (y),
			[result] "r" (result),
			[buffer] "e" (buffer),
			[stride] "M" (BufferBitPlaneStride),
			[length] "M" (BufferBitPlaneLength)
		:	"r23", "r26", "r27"
	);
	return result;
}

// Reads a pixel value from a buffer
// Clips to the buffer bounds (returns 0 for out or bounds reads)
inline unsigned char GetPix(unsigned char x, unsigned char y, unsigned char *buffer)
{
	if(x < BufferWidth && y < BufferHeight)
	{
		return GetPixUnsafe(x, y, buffer);
	}
	else
	{
		return 0;
	}
}

#endif /* DISPLAY_H_ */