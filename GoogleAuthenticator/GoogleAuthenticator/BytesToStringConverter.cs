// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BytesToStringConverter.cs" company="GitHub">
//   Lars Truijens, Sourodeep Chatterjee
// </copyright>
// <summary>
//   Defines the BytesToStringConverter type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace GoogleAuthenticator
{
    using System;
    using System.Globalization;
    using System.Runtime.Remoting;
    using System.Runtime.Remoting.Metadata.W3cXsd2001;
    using System.Windows.Data;
    using System.Windows.Markup;

    /// <summary>
    /// The bytes to string converter.
    /// </summary>
    [ValueConversion(typeof(byte[]), typeof(string))]
    public class BytesToStringConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// The convert.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <param name="targetType">
        /// The target type.
        /// </param>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        /// <param name="culture">
        /// The culture.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            this.BytesToString((byte[])value);

        /// <summary>
        /// The convert back.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <param name="targetType">
        /// The target type.
        /// </param>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        /// <param name="culture">
        /// The culture.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && ((string)value).Length % 2 == 0)
            {
                return this.StringToBytes((string)value);
            }

            return Binding.DoNothing;
        }

        /// <summary>
        /// The provide value.
        /// </summary>
        /// <param name="serviceProvider">
        /// The service provider.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        // http://stackoverflow.com/a/2556329/1242

        /// <summary>
        /// The string to bytes.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <returns>
        /// The <see cref="byte"/>.
        /// </returns>
        public byte[] StringToBytes(string value)
        {
            try
            {
                var shb = SoapHexBinary.Parse(value);
                return shb.Value;
            }
            catch (RemotingException)
            {
                return new byte[0];
            }
        }

        /// <summary>
        /// The bytes to string.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public string BytesToString(byte[] value)
        {
            var shb = new SoapHexBinary(value);
            return shb.ToString();
        }
    }
}
