# LED Badge

    Speed = 8,000,000
    Target Frame Rate = 60
    
    Segments = 12
    Pixels per Segment = 36
    Brightness Passes = 8 # this is with the 1, 4, 8 gamma spread
    
    Frame Width = 36
    Frame Height = 12
    Pixels = Frame Width * Frame Height => 432
    Pixel Blocks = floor((Frame Width + 7) / 8) * Frame Height => 60
    
    Bits per Pixel Compressed = 2 bits
    Bits per Pixel Uncompressed = 3 bits
    Pixels per Byte Compressed = 8 bits / Bits per Pixel Compressed  => 4
    Pixels per Byte Uncompressed = 8 bits / Bits per Pixel Uncompressed => 2.6667
    
    Bytes per Frame Compressed = Pixels / Pixels per Byte Compressed in bytes => 108 bytes
    Bytes per Frame Uncompressed = Pixels / Pixels per Byte Uncompressed in bytes => 162 bytes
    
    Measured Segment Cycles = 259 # measured
    Refresh Interval = 440
    Refresh Rate = Speed / Refresh Interval / Segments / Brightness Passes => 189.3939
    
    Cycles Left Over = Speed * ((Refresh Interval - Measured Segment Cycles) / Refresh Interval) => 3,290,909.0909
    Cycles per Frame = Cycles Left Over / Target Frame Rate => 54,848.4848
    Cycles per Pixel = Cycles per Frame / Pixels => 126.9641
    Cycle Ratio = Cycles Left Over / Speed => 0.4114
    
    Measured SetPix Cycles = 35
    Measured GetPix Cycles = 31
    Measured ClearBuffer Cycles = 908
    Measured SolidFill Cycles = 1921
    Measured Fill Cycles = 5754
    Measured Copy Cycles = 2094
    
    Est Cycles per Fill = Pixel Blocks * Measured SetPix Cycles => 2,100
    Est Cycles per Copy = Pixel Blocks * (Measured GetPix Cycles + Measured SetPix Cycles) => 3,960
    
    Est Longest Op = Measured Fill Cycles / Cycle Ratio => 13,987.6243
    Fills per Frame = Cycles per Frame / Measured Fill Cycles => 9.5322
    Copies per Frame = Cycles per Frame / Measured Copy Cycles => 26.1932
   
# Bandwidth
    
    Video Bandwidth = Target Frame Rate * Bytes per Frame Compressed => 6,480 bytes
    Command Bandwidth = Target Frame Rate * 6 in bytes => 360 bytes
    Bandwidth = Video Bandwidth + Command Bandwidth => 6,840 bytes
    Bits Per Baud = 1 + 8 + 1
    Min Baud = Bandwidth * (Bits Per Baud / 8) / sec in baud => 68,400 baud
    Baud = 38400
    
    Bytes per Frame = (Baud / Bits Per Baud) / Target Frame Rate => 64
    Cycles per Byte = Speed / (Baud / Bits Per Baud) => 2,083.3333
    Queue Size = Est Longest Op / Cycles per Byte => 6.7141
    
    UBBR(Baud) = Speed / (8 * Baud) - 1
    Selected UBBR = UBBR(Baud) => 25.0417
    Selected Rounded UBBR = round(Selected UBBR) => 25
    1 - (Selected UBBR / Selected Rounded UBBR) in percent => -0.1667%

# I2C Bandwidth

    I2C Bandwidth = 200000
    I2C Bytes per Page = 8
    I2C Page Read Overhead = 20
    I2C Pages per Second = I2C Bandwidth / ((8 + 1) * I2C Bytes per Page + I2C Page Read Overhead) => 2,173.913
    I2C Bytes per Second = I2C Pages per Second * I2C Bytes per Page => 17,391.3043
    I2C Bytes per Frame = I2C Bytes per Second / Target Frame Rate => 289.8551
    I2C Frames per Frame = (I2C Bytes per Frame in bytes) / Bytes per Frame Compressed => 2.6838
    
    I2C TWBR Value = ((Speed / I2C Bandwidth) - 16) / 1 / 2 => 12
    Speed / (16 + 2*I2C TWBR Value*1) => 200,000