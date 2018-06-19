# LED Badge

    Speed = 8,000,000
    Target Frame Rate = 30
    
    Segments = 12
    Pixels per Segment = 36
    Brightness Passes = 8 # this is with the 1, 4, 8 gamma spread
    
    Frame Width = 36
    Frame Height = 12
    Pixels = Frame Width * Frame Height => 432
    Pixel Blocks = floor((Frame Width + 7) / 8) * Frame Height => 60
    Padded Pixels = Pixel Blocks * 8 => 480
    
    Bits per Pixel Compressed = 2 bits
    Bits per Pixel Uncompressed = 3 bits
    Pixels per Byte Compressed = 8 bits / Bits per Pixel Compressed  => 4
    Pixels per Byte Uncompressed = 8 bits / Bits per Pixel Uncompressed => 2.6667
    
    Bytes per Frame Compressed = Pixels / Pixels per Byte Compressed in bytes => 108 bytes
    Bytes per Frame Uncompressed = Pixels / Pixels per Byte Uncompressed in bytes => 162 bytes
    
    Measured Segment Cycles = 259 # measured
    Refresh Interval = 352
    Refresh Rate = Speed / Refresh Interval / Segments / Brightness Passes => 236.7424
    
    Cycles Left Over = Speed * ((Refresh Interval - Measured Segment Cycles) / Refresh Interval) => 2,113,636.3636
    Cycles per Frame = Cycles Left Over / Target Frame Rate => 70,454.5455
    Cycles per Pixel = Cycles per Frame / Pixels => 163.0892
    Cycle Ratio = Cycles Left Over / Speed => 0.2642
    
    Measured SetPixBlock Cycles = 35
    Measured GetPixBlock Cycles = 31
    Measured ClearBuffer Cycles = 908
    Measured SolidFill Cycles = 1921
    Measured Fill Cycles = 5754
    Measured Copy Cycles = 2094
    
    Est Cycles per Fill = Pixel Blocks * Measured SetPixBlock Cycles => 2,100
    Est Cycles per Copy = Pixel Blocks * (Measured GetPixBlock Cycles + Measured SetPixBlock Cycles) => 3,960
    
    Est Longest Op = Measured Fill Cycles / Cycle Ratio => 21,778.5806
    Fills per Frame = Cycles per Frame / Measured Fill Cycles => 12.2444
    Copies per Frame = Cycles per Frame / Measured Copy Cycles => 33.6459
   
# Bandwidth
    
    Video Bandwidth = Target Frame Rate * Bytes per Frame Compressed => 3,240 bytes
    Command Bandwidth = Target Frame Rate * 6 in bytes => 180 bytes
    Bandwidth = Video Bandwidth + Command Bandwidth => 3,420 bytes
    Bits Per Baud = 1 + 8 + 1
    Min Baud = Bandwidth * (Bits Per Baud / 8) / sec in baud => 34,200 baud
    Baud = 38400
    
    Bytes per Frame = (Baud / Bits Per Baud) / Target Frame Rate => 128
    Cycles per Byte = Speed / (Baud / Bits Per Baud) => 2,083.3333
    Queue Size = Est Longest Op / Cycles per Byte => 10.4537
    
    UBBR(Baud) = Speed / (16 * Baud) - 1
    Selected UBBR = UBBR(Baud) => 12.0208
    Selected Rounded UBBR = round(Selected UBBR) => 12
    1 - (Selected UBBR / Selected Rounded UBBR) in percent => -0.1736%

# I2C Bandwidth

    I2C Bandwidth = 200000
    I2C Bytes per Page = 8
    I2C Page Read Overhead = 20
    I2C Pages per Second = I2C Bandwidth / ((8 + 1) * I2C Bytes per Page + I2C Page Read Overhead) => 2,173.913
    I2C Bytes per Second = I2C Pages per Second * I2C Bytes per Page => 17,391.3043
    I2C Bytes per Frame = I2C Bytes per Second / Target Frame Rate => 579.7101
    I2C Frames per Frame = (I2C Bytes per Frame in bytes) / Bytes per Frame Compressed => 5.3677
    
    I2C TWBR Value = ((Speed / I2C Bandwidth) - 16) / 1 / 2 => 12
    Speed / (16 + 2*I2C TWBR Value*1) => 200,000