using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace LedBadge
{
    enum DisplayMode
    {
        Nothing,
        Messages,
        TestFill,
        TestCopy,
        TestFrame
    }

    class MainViewModel: INotifyPropertyChanged
    {
        public MainViewModel(Dispatcher dispatcher, Action<UIElement> logFunc)
        {
            Dispatcher = dispatcher;
            LogFunc = logFunc;

            QueryComPorts();

            InitScene(LedBadgeLib.Badges.B1248);

            m_frameTimer.Start();

            HoldTimingA = 1;
            HoldTimingB = 3;
            HoldTimingC = 4;
            IdleFade = true;
            IdleResetToBootImage = true;
            IdleTimeout = 255;

            var badgeDispatcher = new LedBadgeLib.BadgeResponsePassthroughDispatcher();
            badgeDispatcher.ResponseHandler += OnBadgeResponse;
            badgeDispatcher.SendFailureHandler += OnBadgeSendFailure;
            m_badgePump = new LedBadgeLib.BadgePump(badgeDispatcher);
            DisplayMode = DisplayMode.Nothing;
            m_badgePump.RenderFrame += OnRenderFrame;
            m_badgePump.FrameReady += OnFrameReady;
            m_badgePump.GenerateCommands += OnGenerateCommands;
            m_badgePump.Start();

            TextProvider = new TextProvider(Dispatcher, m_messageScene.Queue);
            ImageProvider = new ImageProvider(Dispatcher, m_messageScene.Queue);
            TwitterProvider = new TwitterProvider(Dispatcher, m_messageScene.Queue);
            RawMovieProvider = new RawMovieProvider(Dispatcher, m_messageScene.Queue);
        }

        public byte HoldTimingA { get; set; }
        public byte HoldTimingB { get; set; }
        public byte HoldTimingC { get; set; }
        public bool IdleFade { get; set; }
        public bool IdleResetToBootImage { get; set; }
        public byte IdleTimeout { get; set; }

        public LedBadgeLib.BadgeCaps Device { get; private set; }
        public byte Brightness { get { return m_badgePump.Brightness; } set { m_badgePump.Brightness = value; } }
        public bool RotateFrame { get { return m_badgePump.RotateFrame; } set { m_badgePump.RotateFrame = value; } }
        public WriteableBitmap FrameBuffer { get; private set; }
        public string[] ComPorts { get; private set; }
        public string SelectedComPort { get; set; }
        public bool Connected { get { return m_badgePump.Connected; } }
        public int Frame { get; private set; }
        public float FrameRate { get; private set; }
        public bool DitherFrame { get { return m_badgePump.Dither; } set { m_badgePump.Dither = value; } }
        
        public bool DitherImages
        {
            get { return ImageProvider.Dither; }
            set
            {
                ImageProvider.Dither = value;
                TwitterProvider.Dither = value;
                RawMovieProvider.Dither = value;
            }
        }

        public DisplayMode DisplayMode 
        {
            get { return m_displayMode; }
            set
            {
                m_displayMode = value;
                m_badgePump.UseFrameBuffer = m_displayMode == DisplayMode.Messages || m_displayMode == DisplayMode.TestFrame;
            }
        }

        public WindowState ViewWindowState { get; set; }

        public TextProvider TextProvider { get; set; }
        public ImageProvider ImageProvider { get; set; }
        public TwitterProvider TwitterProvider { get; set; }
        public RawMovieProvider RawMovieProvider { get; set; }

        public Dispatcher Dispatcher { get; set; }
        public Action<UIElement> LogFunc { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        DisplayMode m_displayMode;
        LedBadgeLib.BadgePump m_badgePump;
        LedBadgeLib.MessageQueueVisual m_messageScene;
        Stopwatch m_frameTimer = new Stopwatch();
        
        void RaiseProperyChanged(string prop)
        {
            var handler = PropertyChanged;
            if(handler != null)
            {
                handler(this, new PropertyChangedEventArgs(prop));
            }
        }

        void InitScene(LedBadgeLib.BadgeCaps device)
        {
            if(m_messageScene != null && m_messageScene.Queue.Device == device)
            {
                return;
            }

            FrameBuffer = new WriteableBitmap(device.Width, device.Height, 96, 96, PixelFormats.Gray8, null);

            m_messageScene = new LedBadgeLib.MessageQueueVisual(device);
            m_messageScene.GetTransition = (a, b) =>
            {
                return new LedBadgeLib.SlidingTransition(device, a, b, LedBadgeLib.SlidingDirection.Left, LedBadgeLib.Easing.None, 60, 0);
            };
            m_messageScene.GetDisplay = e =>
            {
                if(e.Element is LedBadgeLib.WpfVisual && ((LedBadgeLib.WpfVisual)e.Element).Element is Image)
                {
                    return new LedBadgeLib.SlidingPosition2D(device, e, 1, 1, LedBadgeLib.Easing.Both, 30, 0, 0);
                }
                else
                {
                    return new LedBadgeLib.SlidingPosition(device, e, LedBadgeLib.SlidingDirection.Left, LedBadgeLib.Easing.None, 60, 0);
                }
            };
            m_messageScene.ExhaustedQueue = (q, lastItem) => 
            { 
                Dispatcher.InvokeAsync(() =>
                    q.Enqueue(LedBadgeLib.WPF.MakeQueuedItem(device, LedBadgeLib.WPF.MakeSingleLineItem(device, "")))); 
                return true; 
            };
        }

        void OnRenderFrame(object sender, LedBadgeLib.BadgeFrameEventArgs args)
        {
            switch(DisplayMode)
            {
                case DisplayMode.Messages:
                {
                    LedBadgeLib.BadgePump pump = (LedBadgeLib.BadgePump)sender;
                    m_messageScene.Update(1.0f / pump.FrameRate);
                    m_messageScene.Render(args.Frame, 0, 0);
                    break;
                }
                case DisplayMode.TestFrame:
                {
                    TestFrame(args.Frame);
                    break;
                }
            }
            Frame++;
        }

        void OnFrameReady(object sender, LedBadgeLib.BadgeFrameEventArgs args)
        {
            bool fpsUpdate = false;
            if(Frame % 60 == 0)
            {
                FrameRate = 60 / (float)m_frameTimer.Elapsed.TotalSeconds;
                m_frameTimer.Restart();
                fpsUpdate = true;
            }

            if(ViewWindowState != WindowState.Minimized)
            {
                Dispatcher.InvokeAsync(() =>
                {
                    LedBadgeLib.WPF.ImageFromPackedBuffer(FrameBuffer, args.Frame.PackedBuffer, 0, RotateFrame, args.Frame.WidthInBlocks, args.Frame.Height, LedBadgeLib.PixelFormat.TwoBits);

                    if(fpsUpdate)
                    {
                        RaiseProperyChanged("FrameRate");
                    }
                });
            }
        }

        void OnGenerateCommands(object sender, LedBadgeLib.BadgeCommandEventArgs args)
        {
            switch(DisplayMode)
            {
                case DisplayMode.TestFill:
                {
                    TestPattern(m_messageScene.Queue.Device, args.CommandStream);
                    break;
                }
                case DisplayMode.TestCopy:
                {
                    TestScroll(m_messageScene.Queue.Device, args.CommandStream);
                    break;
                }
            }
        }

        void TestPattern(LedBadgeLib.BadgeCaps device, MemoryStream commands)
        {
            int bufferSize;
            LedBadgeLib.BadgeCommands.CreateWriteRect(commands, LedBadgeLib.Target.BackBuffer, LedBadgeLib.PixelFormat.TwoBits, 0, 0, (byte)device.WidthInBlocks, 2, out bufferSize);
            for(int i = 0; i < bufferSize; i += 2)
            {
                commands.WriteByte(0x33);
                commands.WriteByte(0x55);
            }

            LedBadgeLib.BadgeCommands.CreateFillRect(commands, LedBadgeLib.Target.BackBuffer, 0, 2, 1, 4, new LedBadgeLib.Pix2x8(0x0000));
            LedBadgeLib.BadgeCommands.CreateFillRect(commands, LedBadgeLib.Target.BackBuffer, 8, 2, 1, 4, new LedBadgeLib.Pix2x8(0x00FF));
            LedBadgeLib.BadgeCommands.CreateFillRect(commands, LedBadgeLib.Target.BackBuffer, 16, 2, 1, 4, new LedBadgeLib.Pix2x8(0xFF00));
            LedBadgeLib.BadgeCommands.CreateFillRect(commands, LedBadgeLib.Target.BackBuffer, 24, 2, 1, 4, new LedBadgeLib.Pix2x8(0xFFFF));

            LedBadgeLib.BadgeCommands.CreateCopyRect(commands, LedBadgeLib.Target.BackBuffer, LedBadgeLib.Target.BackBuffer, 0, 0, 0, 6, (byte)device.WidthInBlocks, 6);

            LedBadgeLib.BadgeCommands.CreateSwap(commands, false, 0);
        }

        void TestScroll(LedBadgeLib.BadgeCaps device, MemoryStream commands)
        {
            LedBadgeLib.BadgeCommands.CreateCopyRect(commands, LedBadgeLib.Target.FrontBuffer, LedBadgeLib.Target.BackBuffer, 0, 0, (byte)(device.WidthInBlocks - 1), (byte)(device.Height - 1), 1, 1);
            LedBadgeLib.BadgeCommands.CreateCopyRect(commands, LedBadgeLib.Target.FrontBuffer, LedBadgeLib.Target.BackBuffer, 0, (byte)(device.Height - 1), (byte)(device.WidthInBlocks - 1), 1, 1, 0);
            LedBadgeLib.BadgeCommands.CreateCopyRect(commands, LedBadgeLib.Target.FrontBuffer, LedBadgeLib.Target.BackBuffer, (byte)(device.WidthInBlocks - 1), 0, 1, (byte)(device.Height - 1), 0, 1);
            LedBadgeLib.BadgeCommands.CreateCopyRect(commands, LedBadgeLib.Target.FrontBuffer, LedBadgeLib.Target.BackBuffer, (byte)(device.WidthInBlocks - 1), (byte)(device.Height - 1), 1, 1, 0, 0);

            LedBadgeLib.BadgeCommands.CreateSwap(commands, false, 0);
        }

        void TestFrame(LedBadgeLib.BadgeRenderTarget frame)
        {
            LedBadgeLib.ScreenCapture.ReadScreenAtMousePosition(frame.IntermediateImage, frame.WidthInPixels, frame.Height);
        }

        void OnBadgeResponse(object sender, LedBadgeLib.BadgeResponseEventArgs args)
        {
            Dispatcher.InvokeAsync(() => LogMessage(args.Code, args.Response), DispatcherPriority.ApplicationIdle);
        }

        void OnBadgeSendFailure(object sender, LedBadgeLib.BadgeSendFailureEventArgs args)
        {
            Dispatcher.InvokeAsync(() => LogMessage("Failed to send packet"), DispatcherPriority.ApplicationIdle);
        }

        void LogMessage(string message, params object[] args)
        {
            LogFunc(new TextBlock() { Text = string.Format(message, args) });
        }

        void LogMessage(LedBadgeLib.ResponseCodes code, byte[] response)
        {
            int scale = 2;
            switch(code)
            {
                case LedBadgeLib.ResponseCodes.Ack:
                {
                    byte cookie;
                    LedBadgeLib.ResponseAckSource source;
                    LedBadgeLib.BadgeResponses.DecodeAck(response, 0, out source, out cookie);
                    LogMessage("{0} [{1}, {2}]", code, source, cookie);
                    break;
                }
                case LedBadgeLib.ResponseCodes.Error:
                {
                    byte cookie;
                    LedBadgeLib.ErrorCodes error;
                    LedBadgeLib.BadgeResponses.DecodeError(response, 0, out error, out cookie);
                    LogMessage("{0} [{1}, {2}]", code, error, cookie);
                    break;
                }
                case LedBadgeLib.ResponseCodes.Setting:
                {
                    LedBadgeLib.SettingValue setting = (LedBadgeLib.SettingValue)(response[0] & 0xF);
                    switch(setting)
                    {
                        case LedBadgeLib.SettingValue.Brightness:
                        {
                            byte brightness;
                            LedBadgeLib.BadgeResponses.DecodeBrightnessSetting(response, 0, out brightness);
                            LogMessage("{0} [{1}, {2}]", code, setting, brightness);
                            break;
                        }
                        case LedBadgeLib.SettingValue.HoldTimings:
                        {
                            byte a, b, c;
                            LedBadgeLib.BadgeResponses.DecodeHoldTimingsSetting(response, 0, out a, out b, out c);
                            LogMessage("{0} [{1}, {2}, {3}, {4}]", code, setting, a, b, c);
                            break;
                        }
                        case LedBadgeLib.SettingValue.IdleTimeout:
                        {
                            byte timeout;
                            bool enableFade;
                            LedBadgeLib.EndofFadeAction endOfFade;
                            LedBadgeLib.BadgeResponses.DecodeIdleTimeoutSetting(response, 0, out timeout, out enableFade, out endOfFade);
                            LogMessage("{0} [{1}, {2}, {3}, {4}]", code, setting, timeout, enableFade, endOfFade);
                            break;
                        }
                        case LedBadgeLib.SettingValue.FadeValue:
                        {
                            byte fadeValue;
                            LedBadgeLib.FadingAction action;
                            LedBadgeLib.BadgeResponses.DecodeFadeValueSetting(response, 0, out fadeValue, out action);
                            LogMessage("{0} [{1}, {2}, {3}]", code, setting, fadeValue, action);
                            break;
                        }
                        case LedBadgeLib.SettingValue.AnimBookmarkPos:
                        {
                            short pos;
                            LedBadgeLib.BadgeResponses.DecodeAnimBookmarkPosSetting(response, 0, out pos);
                            LogMessage("{0} [{1}, {2}]", code, setting, pos);
                            break;
                        }
                        case LedBadgeLib.SettingValue.AnimReadPos:
                        {
                            short pos;
                            LedBadgeLib.BadgeResponses.DecodeAnimReadPosSetting(response, 0, out pos);
                            LogMessage("{0} [{1}, {2}]", code, setting, pos);
                            break;
                        }
                        case LedBadgeLib.SettingValue.AnimPlayState:
                        {
                            LedBadgeLib.AnimState animState;
                            LedBadgeLib.BadgeResponses.DecodeAnimPlayStateSetting(response, 0, out animState);
                            LogMessage("{0} [{1}, {2}]", code, setting, animState);
                            break;
                        }
                        case LedBadgeLib.SettingValue.ButtonState:
                        {
                            bool b0, b1;
                            LedBadgeLib.BadgeResponses.DecodeButtonStateSetting(response, 0, out b0, out b1);
                            LogMessage("{0} [{1}, {2}, {3}]", code, setting, b0, b1);
                            break;
                        }
                        case LedBadgeLib.SettingValue.BufferFullness:
                        {
                            byte fullness;
                            LedBadgeLib.BadgeResponses.DecodeBufferFullnessSetting(response, 0, out fullness);
                            LogMessage("{0} [{1}, {2}]", code, setting, fullness);
                            break;
                        }
                        case LedBadgeLib.SettingValue.Caps:
                        {
                            byte version;
                            byte width;
                            byte height;
                            byte bitDepth;
                            LedBadgeLib.SupportedFeatures capBits;
                            LedBadgeLib.BadgeResponses.DecodeCapsSetting(response, 0, out version, out width, out height, out bitDepth, out capBits);
                            LogMessage("{0} [{1}, {2}, {3}, {4}, {5}, {6}]", code, setting, version, width, height, bitDepth, capBits);
                            break;
                        }
                        default:
                        {
                            LogMessage("{0} [{1}]", code, setting);
                            break;
                        }
                    }
                    break;
                }
                case LedBadgeLib.ResponseCodes.Pixels:
                {
                    LedBadgeLib.PixelFormat format;
                    byte widthInBlocks;
                    byte height; 
                    byte bufferLength;
                    int offset = LedBadgeLib.BadgeResponses.DecodePixels(response, 0, out format, out widthInBlocks, out height, out bufferLength);
                    var img = new System.Windows.Controls.Image()
                    {
                        Source = LedBadgeLib.WPF.ImageFromPackedBuffer(response, offset, RotateFrame, widthInBlocks, height, format),
                        Width = widthInBlocks * LedBadgeLib.BadgeCaps.PixelsPerBlockBitPlane * scale,
                        Height = height * scale,
                        SnapsToDevicePixels = true,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                    };
                    RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
                    LogFunc(new StackPanel()
                    {
                        Children = 
                        {
                            new TextBlock() { Text = string.Format("{0} [{1} {2}x{3}, {4}b]", code, format, widthInBlocks*LedBadgeLib.BadgeCaps.PixelsPerBlockBitPlane, height, bufferLength) },
                            img 
                        }
                    });
                    break;
                }
                case LedBadgeLib.ResponseCodes.Memory:
                {
                    byte numDWords;
                    short address;
                    byte bufferLength;
                    int offset = LedBadgeLib.BadgeResponses.DecodeMemory(response, 0, out numDWords, out address, out bufferLength);

                    StringBuilder sb = new StringBuilder();
                    int p = offset;
                    int x = 0;
                    string hex = "0123456789ABCDEF";
                    char[] buffer = new char[8 * 3 + 1 + 8];
                    for(int i = 0; i < buffer.Length; ++i)
                    {
                        buffer[i] = ' ';
                    }
                    for(int i = 0; i < bufferLength; ++i)
                    {
                        byte b = response[i + offset];
                        buffer[x * 3] = hex[b >> 4];
                        buffer[x * 3 + 1] = hex[b & 0xF];
                        buffer[x + (8 * 3 + 1)] = (b < 32 || b > 127) ? '.' : (char)b;
                        if(++x == 8)
                        {
                            x = 0;
                            sb.Append(buffer);
                            if(i != bufferLength - 1)
                            {
                                sb.AppendLine();
                            }
                        }
                    }
                    if(x != 0)
                    {
                        while(x < 8)
                        {
                            buffer[x * 3] = ' ';
                            buffer[x * 3 + 1] = ' ';
                            buffer[x + (8 * 3 + 1)] = ' ';
                            ++x;
                        }
                        sb.Append(buffer);
                    }

                    LogMessage("{0} [{1}, 0x{2:X4}, {3}]\n{4}", code, numDWords, address, bufferLength, sb.ToString());
                    break;
                }
                default:
                {
                    LogMessage(code.ToString());
                    break;
                }
            }
        }

        public void QueryComPorts()
        {
            ComPorts = SerialPort.GetPortNames();
            RaiseProperyChanged("ComPorts");
        }

        public void Disconnect()
        {
            if(Connected)
            {
                m_badgePump.Disconnect();
                RaiseProperyChanged("Connected");
            }
        }

        public void Connect()
        {
            if(!Connected && !string.IsNullOrEmpty(SelectedComPort))
            {
                try
                {
                    m_badgePump.Connect(SelectedComPort, LedBadgeLib.Badges.B1248);
                    LogMessage("Connected to " + SelectedComPort);
                }
                catch(Exception e)
                {
                    LogMessage("Error connecting: " + e);
                }
                RaiseProperyChanged("Connected");
            }
        }

        public void ToggleConnection()
        {
            if(Connected)
            {
                Disconnect();
            }
            else
            {
                Connect();
            }
        }

        public void SendPing()
        {
            var commands = new MemoryStream();
            LedBadgeLib.BadgeCommands.CreatePing(commands, true, 0xFF);
            m_badgePump.EnqueueCommandsAsync(commands, true);
        }
    }
}
