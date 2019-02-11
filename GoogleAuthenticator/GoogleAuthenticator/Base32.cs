// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Base32.cs" company="GitHub">
//   Lars Truijens, Sourodeep Chatterjee
// </copyright>
// <summary>
//   The base 32.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace GoogleAuthenticator
{
    using System;

    using GoogleAuthenticator.Properties;

    // http://stackoverflow.com/a/7135008/1242

    /// <summary>
    /// The base 32.
    /// </summary>
    public static class Base32
    {
        /// <summary>
        /// The to bytes.
        /// </summary>
        /// <param name="input">
        /// The input.
        /// </param>
        /// <returns>
        /// The <see cref="byte"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// throws if Argument Null Exception
        /// </exception>
        public static byte[] ToBytes(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentNullException(nameof(input));
            }

            input = input.TrimEnd('='); // remove padding characters
            var byteCount = input.Length * 5 / 8; // this must be TRUNCATED
            var returnArray = new byte[byteCount];

            byte curByte = 0, bitsRemaining = 8;
            var arrayIndex = 0;

            foreach (var c in input)
            {
                var cValue = CharToValue(c);

                var mask = 0;
                if (bitsRemaining > 5)
                {
                    mask = cValue << (bitsRemaining - 5);
                    curByte = (byte)(curByte | mask);
                    bitsRemaining -= 5;
                }
                else
                {
                    mask = cValue >> (5 - bitsRemaining);
                    curByte = (byte)(curByte | mask);
                    returnArray[arrayIndex++] = curByte;
                    curByte = (byte)(cValue << (3 + bitsRemaining));
                    bitsRemaining += 3;
                }
            }

            // if we didn't end with a full byte
            if (arrayIndex != byteCount)
            {
                returnArray[arrayIndex] = curByte;
            }

            return returnArray;
        }

        /// <summary>
        /// The to string.
        /// </summary>
        /// <param name="input">
        /// The input.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// throws if Argument Null Exception
        /// </exception>
        public static string ToString(byte[] input)
        {
            if (input == null || input.Length == 0)
            {
                throw new ArgumentNullException(nameof(input));
            }

            var charCount = (int)Math.Ceiling(input.Length / 5d) * 8;
            var returnArray = new char[charCount];

            byte nextChar = 0, bitsRemaining = 5;
            var arrayIndex = 0;

            foreach (var b in input)
            {
                nextChar = (byte)(nextChar | (b >> (8 - bitsRemaining)));
                returnArray[arrayIndex++] = ValueToChar(nextChar);

                if (bitsRemaining < 4)
                {
                    nextChar = (byte)((b >> (3 - bitsRemaining)) & 31);
                    returnArray[arrayIndex++] = ValueToChar(nextChar);
                    bitsRemaining += 5;
                }

                bitsRemaining -= 3;
                nextChar = (byte)((b << bitsRemaining) & 31);
            }

            // if we didn't end with a full char
            if (arrayIndex != charCount)
            {
                returnArray[arrayIndex++] = ValueToChar(nextChar);
                while (arrayIndex != charCount) 
                {
                    returnArray[arrayIndex++] = '='; // padding
                }
            }

            return new string(returnArray);
        }

        /// <summary>
        /// The char to value.
        /// </summary>
        /// <param name="c">
        /// The c.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// throws if Argument Null Exception
        /// </exception>
        private static int CharToValue(char c)
        {
            var value = (int)c;

            // 65-90 == uppercase letters
            if (value < 91 && value > 64)
            {
                return value - 65;
            }

            // 50-55 == numbers 2-7
            if (value < 56 && value > 49)
            {
                return value - 24;
            }

            // 97-122 == lowercase letters
            if (value < 123 && value > 96)
            {
                return value - 97;
            }

            throw new ArgumentException(Resources.Base32_CharToValue_Character_is_not_a_Base32_character_, nameof(c));
        }

        /// <summary>
        /// The value to char.
        /// </summary>
        /// <param name="b">
        /// The b.
        /// </param>
        /// <returns>
        /// The <see cref="char"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// throws if Argument Null Exception
        /// </exception>
        private static char ValueToChar(byte b)
        {
            if (b < 26)
            {
                return (char)(b + 65);
            }

            if (b < 32)
            {
                return (char)(b + 24);
            }

            throw new ArgumentException(Resources.Base32_ValueToChar_Byte_is_not_a_value_Base32_value_, nameof(b));
        }
    }
}
