#ifndef COMMANDS_H_
#define COMMANDS_H_

#define VERSION 1

struct CommandCodes
{
	enum Enum
	{
        Nop,
		Ping,
		Version,
        Swap,
		PollInputs,
        SetBrightness,
        SetPix,
		GetPix,
		GetPixRect,
        SolidFill,
        Fill,
        Copy,
		SetPowerOnImage,
		SetHoldTimings
	};
};

struct ResponseCodes
{
	enum Enum
	{
        Nop,
        Ack,
		Version,
		Pix,
		PixRect,
		Inputs,
		BadCommand,
		ReceiveOverflow
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