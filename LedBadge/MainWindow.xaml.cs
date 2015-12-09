using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LedBadge
{
    public partial class MainWindow: Window
    {
        public MainWindow()
        {
            InitializeComponent();

            SingleLineLayout.IsChecked = true;
            m_vm = new MainViewModel(Dispatcher, LogMessage);
            DataContext = m_vm;
        }

        MainViewModel m_vm;

        void LogMessage(UIElement messageElement)
        {
            int maxMessages = 128;
            if(Log.Children.Count >= maxMessages)
            {
                Log.Children.RemoveRange(0, Log.Children.Count - maxMessages + 1);
            }
            Log.Children.Add(messageElement);
            LogScroller.ScrollToBottom();
        }

        private void ComPortsDropDownOpened(object sender, EventArgs e)
        {
            m_vm.QueryComPorts();
        }

        private void ToggleConnection(object sender, RoutedEventArgs e)
        {
            m_vm.ToggleConnection();
        }

        private void SendPing(object sender, RoutedEventArgs e)
        {
            m_vm.SendPing();
        }

        private void GetVersion(object sender, RoutedEventArgs e)
        {
            m_vm.GetVersion();
        }

        private void PollInputs(object sender, RoutedEventArgs e)
        {
            m_vm.PollInputs();
        }

        private void GetImage(object sender, RoutedEventArgs e)
        {
            m_vm.GetImage();
        }

        private void GetBuffer(object sender, RoutedEventArgs e)
        {
            m_vm.GetBufferState();
        }

        private void SetBootImage(object sender, RoutedEventArgs e)
        {
            m_vm.SetBootImage();
        }

        private void SetHoldTimings(object sender, RoutedEventArgs e)
        {
            m_vm.SetHoldTimings(m_vm.HoldTimingA, m_vm.HoldTimingB, m_vm.HoldTimingC);
        }

        private void SetIdleTimeout(object sender, RoutedEventArgs e)
        {
            m_vm.SetIdleTimeout(m_vm.IdleFade, m_vm.IdleResetToBootImage, m_vm.IdleTimeout);
        }

        private void ClearLog(object sender, RoutedEventArgs e)
        {
            Log.Children.Clear();
        }

        private void SendText(object sender, RoutedEventArgs e)
        {
            var layout = 
                (bool)SingleLineLayout.IsChecked ? Layout.SingleLine :
                (bool)DoubleLineLayout.IsChecked ? Layout.DoubleLine : 
                                                   Layout.Split;
            m_vm.TextProvider.SendText(TextInput.Text, layout);
            TextInput.Clear();
        }

        private void SendImage(object sender, RoutedEventArgs e)
        {
            foreach(string path in ImageInput.Text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                m_vm.ImageProvider.SendImage(path);
            }
            ImageInput.Clear();
        }

        private void ToggleTwitter(object sender, RoutedEventArgs e)
        {
            if(m_vm.TwitterProvider.Running)
            {
                m_vm.TwitterProvider.Stop();
            }
            else
            {
                m_vm.TwitterProvider.Start(Hashtags.Text);
            }

            TwitterButton.Content = m_vm.TwitterProvider.Running ? "Stop" : "Start";
        }
    }
}
