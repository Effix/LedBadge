#if defined(__AVR_ATmega88PA__)

#if !defined(SELECT_ROW) || (SELECT_ROW < 0) || (SELECT_ROW > 23)
#error SELECT_ROW must be defined to a value 0-23 to output pixels!
#endif

	// roughly pseudo-code for the unrolled loop below...
	/*for(unsigned char b = 0, r = 0; b < 6; ++b)
	{
		char data = *g_DisplayReg.Buffer--;
		
		for(unsigned char p = 0; p < 4; ++p, ++r)
		{
			// row select
			if((rowSel + r) != row) 
			{ 
				PORTD |= (1 << PORTD7); 
			} 
			else 
			{ 
				PORTD &= ~(1 << PORTD7); 
			} 

			// column pixels
			if((data & 0b11) > g_DisplayReg.Bright) 
			{ 
				PORTB |= (1 << PORTB1); 
			} 
			else 
			{ 
				PORTB &= ~(1 << PORTB1); 
			} 
			data >>= 2;

			PORTB |= (1 << PORTB0); // shift register clock high
			PORTB &= ~(1 << PORTB0); // shift register clock low
		}
	}*/

#define NextWord() asm volatile ( \
		"ld		 __tmp_reg__, -%a[buffer]"		"\n\t"	/* load next 8 pixels and move pointer to previous byte */ \
		:   [buffer] "+e" (b)							\
		:	/* none */									\
	);

#define OutputPix(BIT) asm volatile ( \
		"bst	__tmp_reg__, %[bit]"			"\n\t"	/* read pixel bit from current byte */ \
		"bld	%[portBReg], 1"					"\n\t"	/* store pixel bit into PB1 */ \
		"out	%[portBAddr], %[portBReg]"		"\n\t"	/* send pixel bit to output (also clocks data-latch low) */ \
		"sbi	%[portBAddr], 0"				"\n\t"	/* clock data-latch high */ \
		:   /* none */									\
		:	[bit] "I" (BIT),							\
			[portBAddr] "I" (_SFR_IO_ADDR(PORTB)),		\
			[portDAddr] "I" (_SFR_IO_ADDR(PORTD)),		\
			[portBReg] "r" (portB)						\
	);

#define OutputPixAndRow(BIT) asm volatile ( \
		"bst	__tmp_reg__, %[bit]"			"\n\t"	/* read pixel bit from current byte */ \
		"bld	%[portBReg], 1"					"\n\t"	/* store pixel bit into PB1 */ \
		"out	%[portBAddr], %[portBReg]"		"\n\t"	/* send pixel bit to output (also clocks data-latch low) */ \
		"out	%[portDAddr], %[portDRegSel]"	"\n\t"	/* select row */ \
		"sbi	%[portBAddr], 0"				"\n\t"	/* clock data-latch high */ \
		"out	%[portDAddr], %[portDRegDef]"	"\n\t"	/* clear row selection */ \
		:   /* none */									\
		:	[bit] "I" (BIT),							\
			[portBAddr] "I" (_SFR_IO_ADDR(PORTB)),		\
			[portDAddr] "I" (_SFR_IO_ADDR(PORTD)),		\
			[portBReg] "r" (portB),						\
			[portDRegDef] "r" (portD_default),			\
			[portDRegSel] "r" (portD_selectRow)			\
	);

{
	const unsigned char *b = g_DisplayReg.BufferP;
	
	NextWord();					

	#if SELECT_ROW == 0
		OutputPixAndRow(0);
	#else
		OutputPix(0);
	#endif
	#if SELECT_ROW == 1
		OutputPixAndRow(1);
	#else
		OutputPix(1);
	#endif
	#if SELECT_ROW == 2
		OutputPixAndRow(2);
	#else
		OutputPix(2);
	#endif
	#if SELECT_ROW == 3
		OutputPixAndRow(3);
	#else
		OutputPix(3);
	#endif
	#if SELECT_ROW == 4
		OutputPixAndRow(4);
	#else
		OutputPix(4);
	#endif
	#if SELECT_ROW == 5
		OutputPixAndRow(5);
	#else
		OutputPix(5);
	#endif
	#if SELECT_ROW == 6
		OutputPixAndRow(6);
	#else
		OutputPix(6);
	#endif
	#if SELECT_ROW == 7
		OutputPixAndRow(7);
	#else
		OutputPix(7);
	#endif

	NextWord();					

	#if SELECT_ROW == 8
		OutputPixAndRow(0);
	#else
		OutputPix(0);
	#endif
	#if SELECT_ROW == 9
		OutputPixAndRow(1);
	#else
		OutputPix(1);
	#endif
	#if SELECT_ROW == 10
		OutputPixAndRow(2);
	#else
		OutputPix(2);
	#endif
	#if SELECT_ROW == 11
		OutputPixAndRow(3);
	#else
		OutputPix(3);
	#endif
	#if SELECT_ROW == 12
		OutputPixAndRow(4);
	#else
		OutputPix(4);
	#endif
	#if SELECT_ROW == 13
		OutputPixAndRow(5);
	#else
		OutputPix(5);
	#endif
	#if SELECT_ROW == 14
		OutputPixAndRow(6);
	#else
		OutputPix(6);
	#endif
	#if SELECT_ROW == 15
		OutputPixAndRow(7);
	#else
		OutputPix(7);
	#endif

	NextWord();					

	#if SELECT_ROW == 16
		OutputPixAndRow(0);
	#else
		OutputPix(0);
	#endif
	#if SELECT_ROW == 17
		OutputPixAndRow(1);
	#else
		OutputPix(1);
	#endif
	#if SELECT_ROW == 18
		OutputPixAndRow(2);
	#else
		OutputPix(2);
	#endif
	#if SELECT_ROW == 19
		OutputPixAndRow(3);
	#else
		OutputPix(3);
	#endif
	#if SELECT_ROW == 20
		OutputPixAndRow(4);
	#else
		OutputPix(4);
	#endif
	#if SELECT_ROW == 21
		OutputPixAndRow(5);
	#else
		OutputPix(5);
	#endif
	#if SELECT_ROW == 22
		OutputPixAndRow(6);
	#else
		OutputPix(6);
	#endif
	#if SELECT_ROW == 23
		OutputPixAndRow(7);
	#else
		OutputPix(7);
	#endif
	
	//g_DisplayReg.BufferP = b;
}

#undef NextWord
#undef OutputPix
#undef OutputPixAndRow
#undef SELECT_ROW

#elif defined(__AVR_ATmega8A__)

#if !defined(SELECT_ROW) || (SELECT_ROW < 0) || (SELECT_ROW > 11)
#error SELECT_ROW must be defined to a value 0-11 to output pixels!
#endif

	// roughly pseudo-code for the unrolled loop below...
	/*
	#define PORTNAME(PORT_LETTER) PORT##PORT_LETTER
	#define PORTPINNAME(PORT_LETTER, PIN_NUMBER) PORT##PORT_LETTER##PIN_NUMBER
	#define SETPIN(PORT_LETTER, PIN_NUMBER, EXPR) PORTNAME(PORT_LETTER) = (PORTNAME(PORT_LETTER) & ~(1 << PORTPINNAME(PORT_LETTER, PIN_NUMBER))) | (EXPR ? (1 << PORTPINNAME(PORT_LETTER, PIN_NUMBER)) : 0)
	#define CLOCKPIN(PORT_LETTER, PIN_NUMBER) PORTNAME(PORT_LETTER) |= (1 << PORTPINNAME(PORT_LETTER, PIN_NUMBER)); PORTNAME(PORT_LETTER) &= ~(1 << PORTPINNAME(PORT_LETTER, PIN_NUMBER))
	#define LATCH(BANK) switch(BANK) { \
		case 0: CLOCKPIN(D, 4); break; \
		case 1: CLOCKPIN(D, 3); break; \
		case 2: CLOCKPIN(C, 2); break; \
		case 3: CLOCKPIN(C, 3); break; \
		case 4: CLOCKPIN(C, 1); break; \
		case 5: CLOCKPIN(C, 0); break; }
	#define DUMP(BANK, D0, D1, D2, D3, D4, D5, D6, D7) { \
		SETPIN(B, 6, D0); \
		SETPIN(B, 7, D1); \
		SETPIN(D, 5, D2); \
		SETPIN(D, 6, D3); \
		SETPIN(B, 2, D4); \
		SETPIN(B, 0, D5); \
		SETPIN(D, 7, D6); \
		SETPIN(B, 1, D7); \
		LATCH(BANK); }
	#define BIT(N) (data & (1 << (N)))
		
	// data and row
	DUMP(5, y != 4, y != 5, y != 6, y != 7, y != 8, y != 9, y != 10, y != 11);
	char data = *--g_DisplayReg.BufferP;
	DUMP(4, BIT(7), BIT(6), BIT(5), BIT(4), y != 0, y != 1, y != 2, y != 3);
	data = *--g_DisplayReg.BufferP;
	DUMP(3, BIT(7), BIT(6), BIT(5), BIT(4), BIT(3), BIT(2), BIT(1), BIT(0));
	data = *--g_DisplayReg.BufferP;
	DUMP(2, BIT(7), BIT(6), BIT(5), BIT(4), BIT(3), BIT(2), BIT(1), BIT(0));
	data = *--g_DisplayReg.BufferP;
	DUMP(1, BIT(7), BIT(6), BIT(5), BIT(4), BIT(3), BIT(2), BIT(1), BIT(0));
	data = *--g_DisplayReg.BufferP;
	DUMP(0, BIT(7), BIT(6), BIT(5), BIT(4), BIT(3), BIT(2), BIT(1), BIT(0));
	*/

// TODO
#undef SELECT_ROW

#endif
