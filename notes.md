# LED Badge

    Speed = 12,000,000
    Target Frame Rate = 60
    
    Segments = 24
    Pixels per Segment = 24
    Brightness Passes = 9 # this is with the 1, 5, 9 gamma spread
    
    Frame Width = 48
    Frame Height = 12
    Pixels = Frame Width * Frame Height => 576
    
    Bits per Pixel Compressed = 2 bits
    Bits per Pixel Uncompressed = 3 bits
    Pixels per Byte Compressed = 8 bits / Bits per Pixel Compressed  => 4
    Pixels per Byte Uncompressed = 8 bits / Bits per Pixel Uncompressed => 2.6667
    
    Bytes per Frame Compressed = Pixels / Pixels per Byte Compressed in bytes => 144 bytes
    Bytes per Frame Uncompressed = Pixels / Pixels per Byte Uncompressed in bytes => 216 bytes
    
    Measured Segment Cycles = 248 # measured
    Refresh Interval = 334
    Refresh Rate = Speed / Refresh Interval / Segments / Brightness Passes => 166.334
    
    Cycles Left Over = Speed * ((Refresh Interval - Measured Segment Cycles) / Refresh Interval) => 3,089,820.3593
    Cycles per Frame = Cycles Left Over / Target Frame Rate => 51,497.006
    Cycle Ratio = Cycles Left Over / Speed => 0.2575
    
    Measured SetPix Cycles = 67
    Measured GetPix Cycles = 28
    Measured ClearBuffer Cycles = 1092
    Measured SolidFill Cycles = 37805
    Measured Fill Cycles = 49990
    Measured Copy Cycles = 90001
    
    Est Cycles per Fill = Pixels * Measured SetPix Cycles => 38,592
    Est Cycles per Copy = Pixels * (Measured GetPix Cycles + Measured SetPix Cycles) => 54,720
    
    Est Longest Op = Measured Copy Cycles / Cycle Ratio => 349,538.7674
    Fills per Frame = Cycles per Frame / Measured Fill Cycles => 1.0301
    Copies per Frame = Cycles per Frame / Measured Copy Cycles => 0.5722
   
# Bandwidth
    
    Video Bandwidth = Target Frame Rate * Bytes per Frame Compressed => 8,640 bytes
    Command Bandwidth = Target Frame Rate * 6 in bytes => 360 bytes
    Bandwidth = Video Bandwidth + Command Bandwidth => 9,000 bytes
    Min Baud = Bandwidth * (10/8) / sec in baud => 90,000 baud
    Baud = 128000
    
    Bytes per Frame = (Baud / 10) / Target Frame Rate => 213.3333
    Cycles per Byte = Speed / (Baud / 10) => 937.5
    Queue Size = Est Longest Op / Cycles per Byte => 372.8414
    
    UBBR(Baud) = Speed / (16 * Baud) - 1
    UBBR(57600) => 12.0208
    UBBR(128000) => 4.8594
    1 - (4.8594 / round(4.8594)) in percent => 2.812%
