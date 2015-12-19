# LED Badge

    Speed = 12,000,000
    Target Frame Rate = 60
    
    Segments = 24
    Pixels per Segment = 24
    Brightness Passes = 8 # this is with the 1, 4, 8 gamma spread
    
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
    Refresh Rate = Speed / Refresh Interval / Segments / Brightness Passes => 187.1257
    
    Cycles Left Over = Speed * ((Refresh Interval - Measured Segment Cycles) / Refresh Interval) => 3,089,820.3593
    Cycles per Frame = Cycles Left Over / Target Frame Rate => 51,497.006
    Cycle Ratio = Cycles Left Over / Speed => 0.2575
    
    Measured SetPix Cycles = 48
    Measured GetPix Cycles = 45
    Measured ClearBuffer Cycles = 1092
    Measured SolidFill Cycles = 27703
    Measured Fill Cycles = 40209
    Measured Copy Cycles = 48603
    
    Est Cycles per Fill = Pixels * Measured SetPix Cycles => 27,648
    Est Cycles per Copy = Pixels * (Measured GetPix Cycles + Measured SetPix Cycles) => 53,568
    
    Est Longest Op = Measured Copy Cycles / Cycle Ratio => 188,760.4884
    Fills per Frame = Cycles per Frame / Measured Fill Cycles => 1.2807
    Copies per Frame = Cycles per Frame / Measured Copy Cycles => 1.0595
   
# Bandwidth
    
    Video Bandwidth = Target Frame Rate * Bytes per Frame Compressed => 8,640 bytes
    Command Bandwidth = Target Frame Rate * 6 in bytes => 360 bytes
    Bandwidth = Video Bandwidth + Command Bandwidth => 9,000 bytes
    Min Baud = Bandwidth * (10/8) / sec in baud => 90,000 baud
    Baud = 128000
    
    Bytes per Frame = (Baud / 10) / Target Frame Rate => 213.3333
    Cycles per Byte = Speed / (Baud / 10) => 937.5
    Queue Size = Est Longest Op / Cycles per Byte => 201.3445
    
    UBBR(Baud) = Speed / (8 * Baud) - 1
    UBBR(57600) => 25.0417
    UBBR(128000) => 10.7188
    Selected UBBR = UBBR(Baud) => 10.7188
    Selected Rounded UBBR = round(Selected UBBR) => 11
    1 - (Selected UBBR / Selected Rounded UBBR) in percent => 2.5568%