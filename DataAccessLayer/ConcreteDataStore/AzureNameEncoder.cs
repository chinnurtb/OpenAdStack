//-----------------------------------------------------------------------
// <copyright file="AzureNameEncoder.cs" company="Rare Crowds Inc.">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;

namespace ConcreteDataStore
{
    /// <summary>Encoding methods for Azure-safe names</summary>
    internal class AzureNameEncoder
    {
        /// <summary>The Xml encoding escape character (valid in Xml names but not frequently appearing)</summary>
        public const char XmlEscapeCharacter = '\u01c1';

        /// <summary>The Azure encoding escape character (valid in Azure names but not frequently appearing)</summary>
        public const char AzureEscapeCharacter = '\u01c2';

        /// <summary>Encode a name to an 'Azure safe' name form.</summary>
        /// <param name="unencodedName">The unencoded name string.</param>
        /// <returns>The Azure name string.</returns>
        public static string EncodeAzureName(string unencodedName)
        {
            if (string.IsNullOrEmpty(unencodedName))
            {
                return unencodedName;
            }

            // See the StringInfo class documentation in MSDN for more details about how .NET
            // handles unicode. The short story is C# strings are sequences of 16-bit values
            // that themselves are an encoding of a unicode string (c#_string.Length != unicode_string.Length).
            // Not all unicode text elements map 1:1 to a 16-bit value. However, our list of legal characters does.
            // For everything else we will encode the 16-bit c# char as a 4-digit hex string. This will
            // round-trip back to the same C# string regardless of what unicode multi-char encoding magic is going on.
            var sb = new StringBuilder();
            foreach (var c in unencodedName)
            {
                sb.Append(EncodeChar(IsAllowedAzureChar, c, AzureEscapeCharacter));
            }

            return sb.ToString();
        }

        /// <summary>Encode a name to an Xml element name.</summary>
        /// <param name="unencodedName">The unencoded name string.</param>
        /// <returns>The Azure name string.</returns>
        public static string EncodeXmlName(string unencodedName)
        {
            if (string.IsNullOrEmpty(unencodedName))
            {
                return unencodedName;
            }

            // First character of xml element name has different rules
            var sb = new StringBuilder();
            sb.Append(EncodeChar(xc => IsAllowedXmlChar(xc, true), unencodedName.Take(1).Single(), XmlEscapeCharacter));
            foreach (var c in unencodedName.Skip(1))
            {
                sb.Append(EncodeChar(xc => IsAllowedXmlChar(xc, false), c, XmlEscapeCharacter));
            }

            return sb.ToString();
        }

        /// <summary>Unencode a name from it's 'Azure safe' form.</summary>
        /// <param name="encodedName">The Azure name string.</param>
        /// <returns>The unencoded .Net unicode name string.</returns>
        public static string UnencodeAzureName(string encodedName)
        {
            return UnencodeName(encodedName, AzureEscapeCharacter);
        }

        /// <summary>Unencode a name from it's 'Azure safe' form.</summary>
        /// <param name="encodedName">The Azure name string.</param>
        /// <returns>The unencoded .Net unicode name string.</returns>
        public static string UnencodeXmlName(string encodedName)
        {
            return UnencodeName(encodedName, XmlEscapeCharacter);
        }

        /// <summary>Unencode a name with the given escape character.</summary>
        /// <param name="encodedName">The name string.</param>
        /// <param name="escapeChar">The escape character to look for.</param>
        /// <returns>The unencoded name string.</returns>
        internal static string UnencodeName(string encodedName, char escapeChar)
        {
            if (string.IsNullOrEmpty(encodedName))
            {
                return encodedName;
            }

            var decodedPropertyName = new StringBuilder();
            for (var i = 0; i < encodedName.Length; i++)
            {
                var c = encodedName[i];
                if (c != escapeChar)
                {
                    decodedPropertyName.Append(c);
                    continue;
                }

                // Get the 4 character escape sequence following the escape character.
                // This will be a 4 digit hex value
                var escapedSequence = encodedName.Substring(i + 1, 4);
                var charCode = Convert.ToInt32(escapedSequence, 16);
                decodedPropertyName.Append(Convert.ToChar(charCode));
                i += 4;
            }

            return decodedPropertyName.ToString();
        }

        /// <summary>Encode a char as an escaped hex string.</summary>
        /// <param name="encodeTest">A function that returns true if the character needs to be encoded.</param>
        /// <param name="charToEncode">The character to encode.</param>
        /// <param name="escapeChar">The escape character to use.</param>
        /// <returns>A string with the unaltered char or the encoded char.</returns>
        private static string EncodeChar(Func<char, bool> encodeTest, char charToEncode, char escapeChar)
        {
            return encodeTest(charToEncode)
                       ? "{0}".FormatInvariant(charToEncode)
                       : "{0}{1:X4}".FormatInvariant(escapeChar, Convert.ToInt32(charToEncode));
        }

        /// <summary>Determine if a character is allowed to be pass unencoded in an Azure name.</summary>
        /// <param name="charToTest">The char to test.</param>
        /// <returns>True if allowed.</returns>
        private static bool IsAllowedAzureChar(char charToTest)
        {
            // The escape character will be encoded
            if (charToTest == AzureEscapeCharacter)
            {
                return false;
            }

            var cat = CharUnicodeInfo.GetUnicodeCategory(charToTest);
            var allowedCategories = new[]
                {
                    UnicodeCategory.UppercaseLetter, 
                    UnicodeCategory.LowercaseLetter, 
                    UnicodeCategory.OtherLetter,
                    UnicodeCategory.DecimalDigitNumber
                };

            // If it's not a category we allow encode it.
            return allowedCategories.Any(c => c == cat);
        }

        /// <summary>Determine if a character is allowed to be pass unencoded in an Azure name.</summary>
        /// <param name="charToTest">The char to test.</param>
        /// <param name="isFirst">True if this is the first char in a name.</param>
        /// <returns>True if allowed.</returns>
        private static bool IsAllowedXmlChar(char charToTest, bool isFirst)
        {
            // The escape character will be encoded
            if (charToTest == XmlEscapeCharacter)
            {
                return false;
            }

            return isFirst 
                ? XmlConvert.IsStartNCNameChar(charToTest) 
                : XmlConvert.IsNCNameChar(charToTest);
        }
    }
}
