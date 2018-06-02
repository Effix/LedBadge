# LED Badge

    Speed = 12,000,000
    Target Frame Rate = 60
    
    Segments = 24
    Pixels per Segment = 24
    Brightness Passes = 8 # this is with the 1, 4, 8 gamma spread
    
    Frame Width = 48
    Frame Height = 12
    Pixels = Frame Width * Frame Height => 576
    Pixel Blocks = floor((Frame Width + 7) / 8) * Frame Height => 72
    
    Bits per Pixel Compressed = 2 bits
    Bits per Pixel Uncompressed = 3 bits
    Pixels per Byte Compressed = 8 bits / Bits per Pixel Compressed  => 4
    Pixels per Byte Uncompressed = 8 bits / Bits per Pixel Uncompressed => 2.6667
    
    Bytes per Frame Compressed = Pixels / Pixels per Byte Compressed in bytes => 144 bytes
    Bytes per Frame Uncompressed = Pixels / Pixels per Byte Uncompressed in bytes => 216 bytes
    
    Measured Segment Cycles = 248 # measured
    Refresh Interval = 336
    Refresh Rate = Speed / Refresh Interval / Segments / Brightness Passes => 186.0119
    
    Cycles Left Over = Speed * ((Refresh Interval - Measured Segment Cycles) / Refresh Interval) => 3,142,857.1429
    Cycles per Frame = Cycles Left Over / Target Frame Rate => 52,380.9524
    Cycles per Pixel = Cycles per Frame / Pixels => 90.9392
    Cycle Ratio = Cycles Left Over / Speed => 0.2619
    
    Measured SetPix Cycles = 38
    Measured GetPix Cycles = 34
    Measured ClearBuffer Cycles = 1088
    Measured SolidFill Cycles = 2148
    Measured Fill Cycles = 7062
    Measured Copy Cycles = 2641
    
    Est Cycles per Fill = Pixel Blocks * Measured SetPix Cycles => 2,736
    Est Cycles per Copy = Pixel Blocks * (Measured GetPix Cycles + Measured SetPix Cycles) => 5,184
    
    Est Longest Op = Measured Fill Cycles / Cycle Ratio => 26,964
    Fills per Frame = Cycles per Frame / Measured Fill Cycles => 7.4173
    Copies per Frame = Cycles per Frame / Measured Copy Cycles => 19.8338
   
# Bandwidth
    
    Video Bandwidth = Target Frame Rate * Bytes per Frame Compressed => 8,640 bytes
    Command Bandwidth = Target Frame Rate * 6 in bytes => 360 bytes
    Bandwidth = Video Bandwidth + Command Bandwidth => 9,000 bytes
    Bits Per Baud = 1 + 8 + 1
    Min Baud = Bandwidth * (Bits Per Baud / 8) / sec in baud => 90,000 baud
    Baud = 115200
    
    Bytes per Frame = (Baud / Bits Per Baud) / Target Frame Rate => 192
    Cycles per Byte = Speed / (Baud / Bits Per Baud) => 1,041.6667
    Queue Size = Est Longest Op / Cycles per Byte => 25.8854
    
    UBBR(Baud) = Speed / (8 * Baud) - 1
    Selected UBBR = UBBR(Baud) => 12.0208
    Selected Rounded UBBR = round(Selected UBBR) => 12
    1 - (Selected UBBR / Selected Rounded UBBR) in percent => -0.1736%
    
# I2C Bandwidth
   
    I2C Bandwidth = 200000
    I2C Bytes per Page = 8
    I2C Page Read Overhead = 20
    I2C Pages per Second = I2C Bandwidth / ((8 + 1) * I2C Bytes per Page + I2C Page Read Overhead) => 2,173.913
    I2C Bytes per Second = I2C Pages per Second * I2C Bytes per Page => 17,391.3043
    I2C Bytes per Frame = I2C Bytes per Second / Target Frame Rate => 289.8551
    I2C Frames per Frame = (I2C Bytes per Frame in bytes) / Bytes per Frame Compressed => 2.0129
    
    I2C TWBR Value = ((Speed / I2C Bandwidth) - 16) / 1 / 2 => 22
    Speed / (16 + 2*I2C TWBR Value*1) => 200,000