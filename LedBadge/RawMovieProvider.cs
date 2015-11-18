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
    class RawMovieProvider
    {
        public RawMovieProvider(Dispatcher dispatcher, LedBadgeLib.MessageQueue messageQueue)
        {
            Dispatcher = dispatcher;
            m_messageQueue = messageQueue;
        }

        public Dispatcher Dispatcher { get; set; }
        public bool Dither { get; set; }
        
        LedBadgeLib.MessageQueue m_messageQueue;
    }
}
