# LedBadge
This project is a custom firmware and library for displaying imagery on a small 48x12 pixel led name badge. 

I am interested in using this as a small notification display attached to a computer and have decided to sacrifice the ability to preload a set of messages to scroll in favor of more robust display support. It needs to be tethered to a computer with this firmware.

##### Some notable features include:
* 4 level gray scale
* 60 Hz frame refresh rate
* 166 Hz display update rate
* 256 levels of brightness in addition to the gray scale (really, a few steps less than that since it has a gamma curve to appear perceptually linear)
* Query button states
* Requesting acknowledgement of commands (setting text on the stock firmware programatically was unreliable)
* Resynchronizing handshake if the command stream is corrupted or overruns
* It can still store a single frame image that will display at start up, even if not tethered

![Samples](https://raw.githubusercontent.com/Effix/LedBadge/master/images/samples.jpg)

# The Hardware
This particular badge is pretty generic and is easy to find under a few different brandings. The hardware is identifiable by two buttons on the back, a mini usb port that is used for data as well as power, and the 48x12 array of leds are packed at a 45 degree angle on the front. I have seen a seller on one popular online retailer even include pictures of the board inside as part of the listing.

##### Notable components:
* Atmel ATMEGA88PA (8 kb program, 1 kb sram, 512 byte internal eeprom)
* Prolific PL2303 USB to serial interface
* 12 MHz crystal oscillator
* 2 tact switches
* 6 74hc595 8-bit shift registers that drive the leds (physically in a 24x24 configuration)
* Atmel serial eeprom (possibly 32 kb, but unused by this firmware)
* ISP header right below the microcontroller

![Badge](https://raw.githubusercontent.com/Effix/LedBadge/master/images/badge.jpg)
![Badge Back](https://raw.githubusercontent.com/Effix/LedBadge/master/images/board_back_sm.png)
![Badge Front](https://raw.githubusercontent.com/Effix/LedBadge/master/images/board_front_sm.png)

I was lucky and got a nice bodge wire hanging out there.

# Wiring

...

# Installing

##### Driver software:
Installation of the Prolific PL2303 serial over usb driver is going to be the same as with the stock setup.

I was unfortunate to get a board with a counterfit PL2303 on it and the newer drivers refuse to initialize it - even the version on the disc that came with the badge. I had to roll way back to a Windows Vista version of the driver to get it to work.

##### Firmware:
You will need an external ISP programmer to update the firmware on the badge. An AVR Dragon works nicely. Be mindful of the ISP header pinout since it may vary between board revisions.

# Libraries

...

![Close](https://raw.githubusercontent.com/Effix/LedBadge/master/images/close.jpg)
