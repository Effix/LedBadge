#ifndef I2C_H_
#define I2C_H_

// Sets up I2C IO
// Called once at program start
void ConfigureI2C();

// Blocks until ACK/NACK from slave
void WaitForI2C();

// Sends start signal
void StartI2C();

// Sends stop signal
void StopI2C();

// Begin writing an asynchronous byte, without blocking
void BeginWriteI2C(unsigned char data);

// Finish writing an asynchronous byte, blocking if needed
void EndWriteI2C();

// Synchronously write a byte
void WriteI2C(unsigned char data);

// Begin reading an asynchronous byte, without blocking
// Set the ack flag to true to continue reading more bytes after this one
void BeginReadI2C(bool ack);

// Finish reading an asynchronous byte, blocking if needed
unsigned char EndReadI2C();

// Synchronously read a byte
// Set the ack flag to true to continue reading more bytes after this one
unsigned char ReadI2C(bool ack);

#endif /* I2C_H_ */