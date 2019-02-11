// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="GitHub">
//   Lars Truijens, Sourodeep Chatterjee
// </copyright>
// <summary>
//   Interaction logic for MainWindow.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------



namespace GoogleAuthenticator
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Windows;
    using System.Windows.Threading;

    /// <summary>
    /// Interaction logic for MainWindow.XAML
    /// </summary>
    /// Converted from http://jsfiddle.net/russau/uRCTk/
    /// Implementation of https://tools.ietf.org/html/rfc6238
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// The seconds to go.
        /// </summary>
        private int secondsToGo;

        /// <summary>
        /// The identity.
        /// </summary>
        private string identity;

        /// <summary>
        /// The secret.
        /// </summary>
        private byte[] secret;

        /// <summary>
        /// The timestamp.
        /// </summary>
        private long timestamp;

        /// <summary>
        /// The h-mac.
        /// </summary>
        private byte[] hmac;

        /// <summary>
        /// The offset.
        /// </summary>
        private int offset;

        /// <summary>
        /// The one time password.
        /// </summary>
        private int oneTimePassword;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            timer.Tick += (s, e) => this.SecondsToGo = 30 - Convert.ToInt32(GetUnixTimestamp() % 30);
            timer.IsEnabled = true;

            this.Secret = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x21, 0xDE, 0xAD, 0xBE, 0xEF };
            this.Identity = "user@host.com";

            this.DataContext = this;
        }

        /// <summary>
        /// The property changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the seconds to go.
        /// </summary>
        public int SecondsToGo
        {
            get => this.secondsToGo;
            private set
            {
                this.secondsToGo = value;
                this.OnPropertyChanged("SecondsToGo");
                if (this.SecondsToGo == 30)
                {
                    this.CalculateOneTimePassword();
                }
            }
        }

        /// <summary>
        /// Gets or sets the identity.
        /// </summary>
        public string Identity
        {
            get => this.identity;
            set
            {
                this.identity = value;
                this.OnPropertyChanged("Identity");
                this.OnPropertyChanged("QRCodeUrl");
                this.CalculateOneTimePassword();
            }
        }

        /// <summary>
        /// Gets or sets the secret base 32.
        /// </summary>
        public string SecretBase32
        {
            get => Base32.ToString(this.Secret);
            set
            {
                try
                {
                    this.Secret = Base32.ToBytes(value);
                }
                catch
                {
                    // ignored
                }

                this.OnPropertyChanged("SecretBase32");
            }
        }

        /// <summary>
        /// Gets or sets the secret.
        /// </summary>
        public byte[] Secret
        {
            get => this.secret;
            set
            {
                this.secret = value;
                this.OnPropertyChanged("Secret");
                this.OnPropertyChanged("QRCodeUrl");
                this.CalculateOneTimePassword();
                this.OnPropertyChanged("SecretBase32");
            }
        }

        /// <summary>
        /// The QR code url.
        /// </summary>
        public string QrCodeUrl => this.GetQrCodeUrl();

        /// <summary>
        /// Gets the timestamp.
        /// </summary>
        public long Timestamp
        {
            get => this.timestamp;
            private set
            {
                this.timestamp = value;
                this.OnPropertyChanged("Timestamp");
            }
        }

        /// <summary>
        /// Gets the h-mac.
        /// </summary>
        public byte[] Hmac
        {
            get => this.hmac;
            private set
            {
                this.hmac = value;
                this.OnPropertyChanged("Hmac");
                this.OnPropertyChanged("HmacPart1");
                this.OnPropertyChanged("HmacPart2");
                this.OnPropertyChanged("HmacPart3");
            }
        }

        /// <summary>
        /// Gets the h-mac part 1.
        /// </summary>
        public byte[] HmacPart1 => this.hmac.Take(this.Offset).ToArray();

        /// <summary>
        /// Gets the h-mac part 2.
        /// </summary>
        public byte[] HmacPart2 => this.hmac.Skip(this.Offset).Take(4).ToArray();

        /// <summary>
        /// Gets the h-mac part 3.
        /// </summary>
        public byte[] HmacPart3 => this.hmac.Skip(this.Offset + 4).ToArray();

        /// <summary>
        /// Gets the offset.
        /// </summary>
        public int Offset
        {
            get => this.offset;
            private set
            {
                this.offset = value;
                this.OnPropertyChanged("Offset");
            }
        }

        /// <summary>
        /// Gets or sets the one time password.
        /// </summary>
        public int OneTimePassword
        {
            get => this.oneTimePassword;
            set
            {
                this.oneTimePassword = value;
                this.OnPropertyChanged("OneTimePassword");
            }
        }

        /// <summary>
        /// The get UNIX timestamp.
        /// </summary>
        /// <returns>
        /// The <see cref="long"/>.
        /// </returns>
        private static long GetUnixTimestamp() =>
            Convert.ToInt64(Math.Round((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds));

        /// <summary>
        /// The get QR code url.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private string GetQrCodeUrl()
        {
            // https://code.google.com/p/google-authenticator/wiki/KeyUriFormat
            return
                $"https://www.google.com/chart?chs=200x200&chld=M|0&cht=qr&chl=otpauth://totp/{this.Identity}%3Fsecret%3D{this.SecretBase32}";
        }

        /// <summary>
        /// The calculate one time password.
        /// </summary>
        private void CalculateOneTimePassword()
        {
            // https://tools.ietf.org/html/rfc4226
            this.Timestamp = Convert.ToInt64(GetUnixTimestamp() / 30); 
            var data = BitConverter.GetBytes(this.Timestamp).Reverse().ToArray();
            this.Hmac = new HMACSHA1(this.Secret).ComputeHash(data);
            this.Offset = this.Hmac.Last() & 0x0F;
            this.OneTimePassword = (
                ((this.Hmac[this.Offset + 0] & 0x7f) << 24) |
                ((this.Hmac[this.Offset + 1] & 0xff) << 16) |
                ((this.Hmac[this.Offset + 2] & 0xff) << 8) |
                (this.Hmac[this.Offset + 3] & 0xff)) % 1000000;
        }

        /// <summary>
        /// The on property changed.
        /// </summary>
        /// <param name="propertyName">
        /// The property name.
        /// </param>
        private void OnPropertyChanged(string propertyName) =>
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
