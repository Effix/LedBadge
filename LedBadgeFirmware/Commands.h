#ifndef COMMANDS_H_
#define COMMANDS_H_

#define VERSION 1

struct CommandCodes
{
	enum Enum
	{
        Nop,				// No-op
		Ping,				// Asking the badge to return the given cookie
		Version,			// Request the version of this firmware
        Swap,				// Swap the front/back render target
		PollInputs,			// Request button state
        SetBrightness,		// Adjust the overall output brightness of the leds
        SetPix,				// Write a single pixel value to a buffer
		GetPix,				// Request a single pixel value from a buffer
		GetPixRect,			// Request a block of pixels from a buffer
        SolidFill,			// Write a single value to a block of pixels in a buffer
        Fill,				// Set a block of pixels in a buffer to the given data (2bpp packed)
        Copy,				// Copy a block of pixels from a location in a buffer to another
		SetPowerOnImage,	// Sets the initial frame when first powered up (saves the front buffer to non-volatile memory)
		SetHoldTimings		// Controls the gray scale levels by setting the hold levels between the bit-planes
	};
};

struct ResponseCodes
{
	enum Enum
	{
        Nop,				// No-op
        Ack,				// Ping response with cookie
		Version,			// Returning the version of this firmware
		Pix,				// Returning a single pixel value
		PixRect,			// Returning a block of pixel values (2bpp packed)
		Inputs,				// Returning the value of the button inputs
		BadCommand,			// Panic response - need to resync
		ReceiveOverflow		// Panic response - need to resync
	};
};

struct Target
{
	enum Enum
    {
        BackBuffer,
        FrontBuffer
    };
};

#endif /* COMMANDS_H_ */