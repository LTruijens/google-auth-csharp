using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Security.Cryptography;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.ComponentModel;
using System.Windows.Threading;

namespace GoogleAuthenticator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    // Converted from http://jsfiddle.net/russau/uRCTk/
    // Implementation of https://tools.ietf.org/html/rfc6238
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();

            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += (s, e) => SecondsToGo = 30 - Convert.ToInt32(GetUnixTimestamp() % 30);
            timer.IsEnabled = true;

            Secret = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x21, 0xDE, 0xAD, 0xBE, 0xEF };
            Identity = "user@host.com";

            DataContext = this;
        }

        private int _secondsToGo;

        public int SecondsToGo
        {
            get { return _secondsToGo; }
            private set { _secondsToGo = value; OnPropertyChanged("SecondsToGo"); if (SecondsToGo == 30) CalculateOneTimePassword(); }
        }


        private string _identity;

        public string Identity
        {
            get { return _identity; }
            set { _identity = value; OnPropertyChanged("Identity"); OnPropertyChanged("QRCodeUrl"); CalculateOneTimePassword(); }
        }

        private byte[] _secret;

        public byte[] Secret
        {
            get { return _secret; }
            set { _secret = value; OnPropertyChanged("Secret"); OnPropertyChanged("QRCodeUrl"); CalculateOneTimePassword(); }
        }

        public string QRCodeUrl
        {
            get { return GetQRCodeUrl(); }
        }

        private Int64 _timestamp;

        public Int64 Timestamp
        {
            get { return _timestamp; }
            private set { _timestamp = value; OnPropertyChanged("Timestamp"); }
        }

        private byte[] _hmac;

        public byte[] Hmac
        {
            get { return _hmac; }
            private set { _hmac = value; OnPropertyChanged("Hmac"); OnPropertyChanged("HmacPart1"); OnPropertyChanged("HmacPart2"); OnPropertyChanged("HmacPart3"); }
        }

        public byte[] HmacPart1
        {
            get { return _hmac.Take(Offset).ToArray(); }
        }

        public byte[] HmacPart2
        {
            get { return _hmac.Skip(Offset).Take(4).ToArray(); }
        }
        
        public byte[] HmacPart3
        {
            get { return _hmac.Skip(Offset + 4).ToArray(); }
        }
        
        private int _offset;

        public int Offset
        {
            get { return _offset; }
            private set { _offset = value; OnPropertyChanged("Offset"); }
        }

        private int _oneTimePassword;

        public int OneTimePassword
        {
            get { return _oneTimePassword; }
            set { _oneTimePassword = value; OnPropertyChanged("OneTimePassword"); }
        }

        private string GetQRCodeUrl()
        {
            // https://code.google.com/p/google-authenticator/wiki/KeyUriFormat
            var base32Secret = Base32.Encode(Secret);
            return String.Format("https://www.google.com/chart?chs=200x200&chld=M|0&cht=qr&chl=otpauth://totp/{0}%3Fsecret%3D{1}", Identity, base32Secret);            
        }

        private void CalculateOneTimePassword()
        {
            // https://tools.ietf.org/html/rfc4226
            Timestamp = Convert.ToInt64(GetUnixTimestamp() / 30); 
            var data = BitConverter.GetBytes(Timestamp).Reverse().ToArray();
            Hmac = new HMACSHA1(Secret).ComputeHash(data);
            Offset = Hmac.Last() & 0x0F;
            OneTimePassword = (
                ((Hmac[Offset + 0] & 0x7f) << 24) |
                ((Hmac[Offset + 1] & 0xff) << 16) |
                ((Hmac[Offset + 2] & 0xff) << 8) |
                (Hmac[Offset + 3] & 0xff)
                    ) % 1000000;
        }

        private static Int64 GetUnixTimestamp()
        {
            return Convert.ToInt64(Math.Round((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
