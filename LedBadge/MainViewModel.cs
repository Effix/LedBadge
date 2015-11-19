using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Tweetinvi.Core.Events.EventArguments;

namespace LedBadge
{
    enum DisplayMode
    {
        Messages,
        TestFill,
        TestCopy
    }

    class MainViewModel: INotifyPropertyChanged
    {
        public MainViewModel(Dispatcher dispatcher, Action<UIElement> logFunc)
        {
            Dispatcher = dispatcher;
            LogFunc = logFunc;

            QueryComPorts();

            FrameBuffer = new WriteableBitmap(LedBadgeLib.BadgeCaps.Width, LedBadgeLib.BadgeCaps.Height, 96, 96, PixelFormats.Gray8, null);
            InitScene();

            m_frameTimer.Start();

            var badgeDispatcher = new LedBadgeLib.BadgeResponsePassthroughDispatcher();
            badgeDispatcher.ResponseHandler += OnBadgeResponse;
            m_badgePump = new LedBadgeLib.BadgePump(badgeDispatcher);
            m_badgePump.RenderFrame += OnRenderFrame;
            m_badgePump.FrameReady += OnFrameReady;
            m_badgePump.GenerateCommands += OnGenerateCommands;
            m_badgePump.Start();

            TextProvider = new TextProvider(Dispatcher, m_messageScene.Queue);
            ImageProvider = new ImageProvider(Dispatcher, m_messageScene.Queue);
            TwitterProvider = new TwitterProvider(Dispatcher, m_messageScene.Queue);
            RawMovieProvider = new RawMovieProvider(Dispatcher, m_messageScene.Queue);
        }

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
                m_badgePump.UseFrameBuffer = m_displayMode == DisplayMode.Messages;
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

        void InitScene()
        {
            m_messageScene = new LedBadgeLib.MessageQueueVisual();
            m_messageScene.GetTransition = (a, b) =>
            {
                return new LedBadgeLib.SlidingTransition(a, b, LedBadgeLib.SlidingDirection.Left, LedBadgeLib.Easing.None, 60, 0);
            };
            m_messageScene.GetDisplay = e =>
            {
                if(e.Element is LedBadgeLib.WpfVisual && ((LedBadgeLib.WpfVisual)e.Element).Element is Image)
                {
                    return new LedBadgeLib.SlidingPosition2D(e, 1, 1, LedBadgeLib.Easing.Both, 30, 0, 0);
                }
                else
                {
                    return new LedBadgeLib.SlidingPosition(e, LedBadgeLib.SlidingDirection.Left, LedBadgeLib.Easing.None, 60, 0);
                }
            };
            m_messageScene.ExhaustedQueue = (q, lastItem) => 
            { 
                Dispatcher.InvokeAsync(() =>
                    q.Enqueue(LedBadgeLib.WPF.MakeQueuedItem(LedBadgeLib.WPF.MakeSingleLineItem("")))); 
                return true; 
            };
        }

        void OnRenderFrame(object sender, LedBadgeLib.BadgeFrameEventArgs args)
        {
            LedBadgeLib.BadgePump pump = (LedBadgeLib.BadgePump)sender;
            m_messageScene.Update(1.0f / pump.FrameRate);
            m_messageScene.Render(args.Frame, 0, 0);
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
                    LedBadgeLib.WPF.ImageFromPackedBuffer(FrameBuffer, args.Frame.PackedBuffer, 0, RotateFrame, args.Frame.Width, args.Frame.Height);

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
                case LedBadge.DisplayMode.TestFill:
                {
                    TestPattern(args.CommandStream);
                    break;
                }
                case LedBadge.DisplayMode.TestCopy:
                {
                    TestScroll(args.CommandStream);
                    break;
                }
            }
        }

        void TestPattern(MemoryStream commands)
        {
            byte[] buffer = new byte[LedBadgeLib.BadgeCaps.FrameSize];
            for(int i = 0; i < buffer.Length; ++i)
            {
                buffer[i] = 0xE4;
            }
            LedBadgeLib.BadgeCommands.FillRect(commands, 0, 0, 48, 2, LedBadgeLib.Target.BackBuffer, buffer);

            LedBadgeLib.BadgeCommands.SolidFillRect(commands,  0, 2, 12, 4, LedBadgeLib.Target.BackBuffer, 0);
            LedBadgeLib.BadgeCommands.SolidFillRect(commands, 12, 2, 12, 4, LedBadgeLib.Target.BackBuffer, 1);
            LedBadgeLib.BadgeCommands.SolidFillRect(commands, 24, 2, 12, 4, LedBadgeLib.Target.BackBuffer, 2);
            LedBadgeLib.BadgeCommands.SolidFillRect(commands, 36, 2, 12, 4, LedBadgeLib.Target.BackBuffer, 3);

            LedBadgeLib.BadgeCommands.CopyRect(commands, 0, 0, 48, 6, 0, 6, LedBadgeLib.Target.BackBuffer, LedBadgeLib.Target.BackBuffer);

            LedBadgeLib.BadgeCommands.Swap(commands);
        }

        void TestScroll(MemoryStream commands)
        {
            LedBadgeLib.BadgeCommands.CopyRect(commands, 0, 0, 47, 11, 1, 1, LedBadgeLib.Target.FrontBuffer, LedBadgeLib.Target.BackBuffer);
            LedBadgeLib.BadgeCommands.CopyRect(commands, 0, 11, 47, 1, 1, 0, LedBadgeLib.Target.FrontBuffer, LedBadgeLib.Target.BackBuffer);
            LedBadgeLib.BadgeCommands.CopyRect(commands, 47, 0, 1, 11, 0, 1, LedBadgeLib.Target.FrontBuffer, LedBadgeLib.Target.BackBuffer);
            LedBadgeLib.BadgeCommands.CopyRect(commands, 47, 11, 1, 1, 0, 0, LedBadgeLib.Target.FrontBuffer, LedBadgeLib.Target.BackBuffer);

            LedBadgeLib.BadgeCommands.Swap(commands);
        }

        void OnBadgeResponse(object sender, LedBadgeLib.BadgeResponseEventArgs args)
        {
            Dispatcher.InvokeAsync(() => LogMessage(args.Code, args.Response));
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
                    int cookie;
                    LedBadgeLib.BadgeResponses.DecodeAck(response, 0, out cookie);
                    LogMessage("{0} [{1}]", code, cookie);
                    break;
                }
                case LedBadgeLib.ResponseCodes.Inputs:
                {
                    bool b0, b1;
                    LedBadgeLib.BadgeResponses.DecodeInputs(response, 0, out b0, out b1);
                    LogMessage("{0} [{1}, {2}]", code, b0, b1);
                    break;
                }
                case LedBadgeLib.ResponseCodes.Pix:
                {
                    int value;
                    LedBadgeLib.BadgeResponses.DecodePix(response, 0, out value);
                    LogFunc(new StackPanel()
                    {
                        Children = 
                        {
                            new TextBlock() { Text = string.Format("{0} [{1}]", code, value) },
                            new System.Windows.Shapes.Rectangle() 
                            {
                                Width = 48 * scale,
                                Height = 12 * scale,
                                Fill = new SolidColorBrush(LedBadgeLib.WPF.ColorFromPix((byte)value)),
                                SnapsToDevicePixels = true,
                                HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                            } 
                        }
                    });
                    break;
                }
                case LedBadgeLib.ResponseCodes.PixRect:
                {
                    int width, height, length;
                    int offset = LedBadgeLib.BadgeResponses.DecodePixRect(response, 0, out width, out height, out length);
                    var img = new System.Windows.Controls.Image()
                    {
                        Source = LedBadgeLib.WPF.ImageFromPackedBuffer(response, offset, RotateFrame, width, height),
                        Width = width * scale,
                        Height = height * scale,
                        SnapsToDevicePixels = true,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                    };
                    RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
                    LogFunc(new StackPanel()
                    {
                        Children = 
                        {
                            new TextBlock() { Text = string.Format("{0} [{1}x{2}, {3}b]", code, width, height, length) },
                            img 
                        }
                    });
                    break;
                }
                case LedBadgeLib.ResponseCodes.Version:
                {
                    int version;
                    LedBadgeLib.BadgeResponses.DecodeVersion(response, 0, out version);
                    LogMessage("{0} [{1}]", code, version);
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
                    m_badgePump.Connect(SelectedComPort);
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
            LedBadgeLib.BadgeCommands.Ping(commands, 0);
            m_badgePump.EnqueueCommandsAsync(commands);
        }

        public void GetVersion()
        {
            var commands = new MemoryStream();
            LedBadgeLib.BadgeCommands.GetVersion(commands);
            m_badgePump.EnqueueCommandsAsync(commands);
        }

        public void PollInputs()
        {
            var commands = new MemoryStream();
            LedBadgeLib.BadgeCommands.PollInputs(commands);
            m_badgePump.EnqueueCommandsAsync(commands);
        }

        public void GetImage()
        {
            var commands = new MemoryStream();
            LedBadgeLib.BadgeCommands.GetPixelRect(commands, 0, 0, LedBadgeLib.BadgeCaps.Width, LedBadgeLib.BadgeCaps.Height, LedBadgeLib.Target.FrontBuffer);
            m_badgePump.EnqueueCommandsAsync(commands);
        }

        public void SetBootImage()
        {
            var commands = new MemoryStream();
            LedBadgeLib.BadgeCommands.SetPowerOnImage(commands);
            LedBadgeLib.BadgeCommands.Ping(commands, 15);
            m_badgePump.EnqueueCommandsAsync(commands);
        }
    }

    class EnumerationExtension: MarkupExtension
    {
        public EnumerationExtension(Type enumType)
        {
            Type = enumType;
        }

        public Type Type { get; private set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Enum.GetValues(Type);
        }
    }
}
