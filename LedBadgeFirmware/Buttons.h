#ifndef BUTTONS_H_
#define BUTTONS_H_

#include <avr/io.h>

// Polls back buttons for input
bool CheckButton0();
bool CheckButton1();

// Sets up button input
// Called once at program start
void ConfigurePushButtons();

#endif /* BUTTONS_H_ */