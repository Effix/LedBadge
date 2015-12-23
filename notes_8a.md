# LED Badge

    Speed = 8,000,000
    Target Frame Rate = 60
    
    Segments = 12
    Pixels per Segment = 36
    Brightness Passes = 8 # this is with the 1, 4, 8 gamma spread
    
    Frame Width = 36
    Frame Height = 12
    Pixels = Frame Width * Frame Height => 432
    
    Bits per Pixel Compressed = 2 bits
    Bits per Pixel Uncompressed = 3 bits
    Pixels per Byte Compressed = 8 bits / Bits per Pixel Compressed  => 4
    Pixels per Byte Uncompressed = 8 bits / Bits per Pixel Uncompressed => 2.6667
    
    Bytes per Frame Compressed = Pixels / Pixels per Byte Compressed in bytes => 108 bytes
    Bytes per Frame Uncompressed = Pixels / Pixels per Byte Uncompressed in bytes => 162 bytes
    
    Measured Segment Cycles = 259 # measured
    Refresh Interval = 440
    Refresh Rate = Speed / Refresh Interval / Segments / Brightness Passes => 189.3939
    
    Cycles Left Over = Speed * ((Refresh Interval - Measured Segment Cycles) / Refresh Interval) => 3,290,909.0909
    Cycles per Frame = Cycles Left Over / Target Frame Rate => 54,848.4848
    Cycle Ratio = Cycles Left Over / Speed => 0.4114
    
    Measured SetPix Cycles = 48
    Measured GetPix Cycles = 45
    Measured ClearBuffer Cycles = 912
    Measured SolidFill Cycles = 20447
    Measured Fill Cycles = 30525
    Measured Copy Cycles = 33523
    
    Est Cycles per Fill = Pixels * Measured SetPix Cycles => 20,736
    Est Cycles per Copy = Pixels * (Measured GetPix Cycles + Measured SetPix Cycles) => 40,176
    
    Est Longest Op = Measured Copy Cycles / Cycle Ratio => 81,492.3757
    Fills per Frame = Cycles per Frame / Measured Fill Cycles => 1.7968
    Copies per Frame = Cycles per Frame / Measured Copy Cycles => 1.6361
   
# Bandwidth
    
    Video Bandwidth = Target Frame Rate * Bytes per Frame Compressed => 6,480 bytes
    Command Bandwidth = Target Frame Rate * 6 in bytes => 360 bytes
    Bandwidth = Video Bandwidth + Command Bandwidth => 6,840 bytes
    Min Baud = Bandwidth * (10/8) / sec in baud => 68,400 baud
    Baud = 115200
    
    Bytes per Frame = (Baud / 10) / Target Frame Rate => 192
    Cycles per Byte = Speed / (Baud / 10) => 694.4444
    Queue Size = Est Longest Op / Cycles per Byte => 117.349
    
    UBBR(Baud) = Speed / (8 * Baud) - 1
    Selected UBBR = UBBR(Baud) => 7.6806
    Selected Rounded UBBR = round(Selected UBBR) => 8
    1 - (Selected UBBR / Selected Rounded UBBR) in percent => 3.9931%
