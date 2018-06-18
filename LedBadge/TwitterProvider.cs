using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace LedBadge
{
    class TwitterProvider
    {
        static readonly string SettingsPath = @".\twitter.settings";

        public TwitterProvider(Dispatcher dispatcher, LedBadgeLib.MessageQueue messageQueue)
        {
            Dispatcher = dispatcher;
            m_messageQueue = messageQueue;

            LoadSettings();
        }

        public bool Running { get { return m_stream != null; } }
        public Dispatcher Dispatcher { get; set; }
        public bool Dither { get; set; }

        public string ConsumerKey 
        {
            get { return m_consumerKey; }
            set
            {
                if(m_consumerKey != value)
                {
                    m_consumerKey = value;
                    SaveSettings();
                }
            }
        }
        public string ConsumerSecret
        {
            get { return m_consumerSecret; }
            set
            {
                if(m_consumerSecret != value)
                {
                    m_consumerSecret = value;
                    SaveSettings();
                }
            }
        }
        public string AccessToken
        {
            get { return m_accessToken; }
            set
            {
                if(m_accessToken != value)
                {
                    m_accessToken = value;
                    SaveSettings();
                }
            }
        }
        public string AccessTokenSecret
        {
            get { return m_accessTokenSecret; }
            set
            {
                if(m_accessTokenSecret != value)
                {
                    m_accessTokenSecret = value;
                    SaveSettings();
                }
            }
        }

        string m_consumerKey;
        string m_consumerSecret;
        string m_accessToken;
        string m_accessTokenSecret;
        LedBadgeLib.MessageQueue m_messageQueue;

        public void Start(string keyWords)
        {
            if(!Running)
            {
                Tweetinvi.Auth.ApplicationCredentials = new Tweetinvi.Core.Credentials.TwitterCredentials(
                    ConsumerKey, ConsumerSecret, AccessToken, AccessTokenSecret);

                var words = keyWords.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                if(words.Any())
                {
                    m_stream = Tweetinvi.Stream.CreateFilteredStream();
                    m_stream.MatchingTweetReceived += OnMatchingTweekReceived;
                    m_stream.StreamStopped += OnStreamStopped;
                    foreach(string word in words)
                    {
                        m_stream.AddTrack(word);
                    }
                    m_streamTask = m_stream.StartStreamMatchingAnyConditionAsync();
                }
            }
        }

        public void Stop()
        {
            if(Running)
            {
                m_stream.StopStream();
                m_stream = null;
                m_streamTask = null;
            }
        }

        void LoadSettings()
        {
            try
            {
                if(File.Exists(SettingsPath))
                {
                    var settings = File.ReadAllLines(SettingsPath);
                    m_consumerKey       = settings.Length > 0 ? settings[0] : "";
                    m_consumerSecret    = settings.Length > 1 ? settings[1] : "";
                    m_accessToken       = settings.Length > 2 ? settings[2] : "";
                    m_accessTokenSecret = settings.Length > 3 ? settings[3] : "";
                }
                else
                {
                    m_consumerKey =
                    m_consumerSecret =
                    m_accessToken =
                    m_accessTokenSecret = "";
                }
            }
            catch(IOException)
            {
            }
        }

        void SaveSettings()
        {
            try
            {
                File.WriteAllLines(SettingsPath, new[] 
                {
                    m_consumerKey,
                    m_consumerSecret,
                    m_accessToken,
                    m_accessTokenSecret
                });
            }
            catch(IOException)
            {
            }
        }

        void OnStreamStopped(object sender, Tweetinvi.Core.Events.EventArguments.StreamExceptionEventArgs args)
        {
            if(args.Exception != null)
            {
                Thread.Sleep(5000);
                m_streamTask = m_stream.StartStreamMatchingAnyConditionAsync();
            }
        }

        void OnMatchingTweekReceived(object sender, Tweetinvi.Core.Events.EventArguments.MatchedTweetReceivedEventArgs args)
        {
            if(args.Tweet.Language == Tweetinvi.Core.Enum.Language.English || args.Tweet.Media.Count > 0)
            {
                Dispatcher.InvokeAsync(() =>
                {
                    if(!args.Tweet.Retweeted)
                    {
                        var images = new List<BitmapImage>();

                        images.Add(new BitmapImage(new Uri(args.Tweet.CreatedBy.ProfileImageUrl, UriKind.Absolute)));
                        foreach(var m in args.Tweet.Media)
                        {
                            //if(m.MediaType == "photo")
                            {
                                images.Add(new BitmapImage(new Uri(m.MediaURL + ":thumb", UriKind.Absolute)));
                            }
                        }

                        Dispatcher.InvokeAsync(() => CheckTweetData(args.Tweet.Text, images), DispatcherPriority.Background);
                    }
                });
            }
        }

        void CheckTweetData(string text, List<BitmapImage> images)
        {
            bool ready = true;
            foreach(var img in images)
            {
                if(img.IsDownloading)
                {
                    ready = false;
                    break;
                }
            }

            if(ready)
            {
                Func<ImageSource, Image> makeImg = source =>
                {
                    var el = new System.Windows.Controls.Image()
                    {
                        Source = source,
                        SnapsToDevicePixels = true,
                        UseLayoutRounding = true,
                        MinWidth = m_messageQueue.Device.Width,
                        MinHeight = m_messageQueue.Device.Height,
                        Width = source.Width * ((BitmapSource)source).DpiX / 96,
                        Stretch = Stretch.UniformToFill,
                    };
                    RenderOptions.SetBitmapScalingMode(source, BitmapScalingMode.NearestNeighbor);
                    el.Measure(new Size(m_messageQueue.Device.Width, m_messageQueue.Device.Height));
                    el.Arrange(new Rect(0, 0, m_messageQueue.Device.Width, m_messageQueue.Device.Height));
                    return el;
                };

                if(images.Count > 0)
                {
                    m_messageQueue.Enqueue(new LedBadgeLib.MessageQueueItem(new LedBadgeLib.WpfVisual(m_messageQueue.Device, makeImg(images[0]), dither : Dither)));
                }
                m_messageQueue.Enqueue(LedBadgeLib.WPF.MakeQueuedItem(m_messageQueue.Device, LedBadgeLib.WPF.MakeSingleLineItem(m_messageQueue.Device, text)));
                for(int i = 1; i < images.Count; ++i)
                {
                    m_messageQueue.Enqueue(new LedBadgeLib.MessageQueueItem(new LedBadgeLib.WpfVisual(m_messageQueue.Device, makeImg(images[i]), dither : Dither)));
                }
            }
            else
            {
                Dispatcher.InvokeAsync(() => CheckTweetData(text, images), DispatcherPriority.Background);
            }
        }

        Task m_streamTask;
        Tweetinvi.Core.Interfaces.Streaminvi.IFilteredStream m_stream;
    }
}
