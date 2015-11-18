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
    enum Layout
    {
        SingleLine,
        DoubleLine,
        Split
    }

    class TextProvider
    {
        public TextProvider(Dispatcher dispatcher, LedBadgeLib.MessageQueue messageQueue)
        {
            Dispatcher = dispatcher;
            m_messageQueue = messageQueue;
        }

        public Dispatcher Dispatcher { get; set; }
        
        LedBadgeLib.MessageQueue m_messageQueue;

        public void SendText(string text, Layout layout)
        {
            var lines = text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            switch(layout)
            {
                case Layout.SingleLine:
                {
                    for(int i = 0; i < lines.Length; ++i)
                    {
                        var msg = lines[i];
                        m_messageQueue.Enqueue(LedBadgeLib.WPF.MakeQueuedItem(LedBadgeLib.WPF.MakeSingleLineItem(msg)));
                    }
                    break;
                }
                case Layout.DoubleLine:
                {
                    for(int i = 0; i < lines.Length; i += 2)
                    {
                        var msg1 = lines[i];
                        var msg2 = i + 1 < lines.Length ? lines[i + 1] : "";
                        m_messageQueue.Enqueue(LedBadgeLib.WPF.MakeQueuedItem(LedBadgeLib.WPF.MakeDoubleLineItem(msg1, msg2)));
                    }
                    break;
                }
                case Layout.Split:
                {
                    for(int i = 0; i < lines.Length; i += 3)
                    {
                        var msg1 = lines[i];
                        var msg2 = i + 1 < lines.Length ? lines[i + 1] : "";
                        var msg3 = i + 2 < lines.Length ? lines[i + 2] : "";
                        m_messageQueue.Enqueue(LedBadgeLib.WPF.MakeQueuedItem(LedBadgeLib.WPF.MakeSplitLineItem(msg1, msg2, msg3)));
                    }
                    break;
                }
            }
        }
    }
}
