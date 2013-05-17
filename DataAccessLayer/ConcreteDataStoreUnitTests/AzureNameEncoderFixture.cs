//-----------------------------------------------------------------------
// <copyright file="AzureNameEncoderFixture.cs" company="Rare Crowds Inc.">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Xml;
using System.Xml.Linq;
using ConcreteDataStore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcreteDataStoreUnitTests
{
    /// <summary>
    /// Test fixture for AzureNameEncoder
    /// </summary>
    [TestClass]
    public class AzureNameEncoderFixture
    {
        /// <summary>
        /// Null returns null
        /// </summary>
        [TestMethod]
        public void EncodeAzureNameNull()
        {
            Assert.IsNull(AzureNameEncoder.EncodeAzureName(null));
        }

        /// <summary>
        /// Empty returns empty
        /// </summary>
        [TestMethod]
        public void EncodeAzureNameEmpty()
        {
            this.AssertEncoding(string.Empty, AzureNameEncoder.EncodeAzureName(string.Empty));
        }

        /// <summary>AlphaNumeric names with no special characters should not change.</summary>
        [TestMethod]
        public void EncodeAzureNameNoEncodedChars()
        {
            // Contains uppercase letter, lowercase letter, decimal, and other letter.
            // These are the unicode character categories that will not be encoded
            var unencodedName = "aAzZ09\u4e00";
            var encodedName = AzureNameEncoder.EncodeAzureName(unencodedName);
            this.AssertEncoding(unencodedName, encodedName);
        }

        /// <summary>
        /// Underscore is allowed by Azure but we use it as a delimiter so make sure
        /// it gets encoded on external names.
        /// </summary>
        [TestMethod]
        public void EncodeAzureNameWithUnderscore()
        {
            var unencodedName = "a_b";
            var expectedEncodedName = "a{0}005Fb".FormatInvariant(AzureNameEncoder.AzureEscapeCharacter);
            var encodedName = AzureNameEncoder.EncodeAzureName(unencodedName);
            this.AssertEncoding(expectedEncodedName, encodedName);
        }

        /// <summary>Encoding escape char gets double escaped.</summary>
        [TestMethod]
        public void EncodeAzureNameWithEscapeCharacter()
        {
            var unencodedName = "a{0}b".FormatInvariant(AzureNameEncoder.AzureEscapeCharacter);
            var expectedEncodedName = "a{0}{1:X4}b"
                .FormatInvariant(AzureNameEncoder.AzureEscapeCharacter, Convert.ToInt32(AzureNameEncoder.AzureEscapeCharacter));
            var encodedName = AzureNameEncoder.EncodeAzureName(unencodedName);
            this.AssertEncoding(expectedEncodedName, encodedName);
        }

        /// <summary>Non-letter/non-decimal characters are encoded.</summary>
        [TestMethod]
        public void EncodeAzureNameNonLetter()
        {
            var unencodedName = "a\u0304 \u9fcf";
            var expectedEncodedName = "a{0}0304{0}0020{0}9FCF".FormatInvariant(AzureNameEncoder.AzureEscapeCharacter);
            var encodedName = AzureNameEncoder.EncodeAzureName(unencodedName);
            this.AssertEncoding(expectedEncodedName, encodedName);
        }

        /// <summary>
        /// Surrogate sequences are encoded
        /// (.Net strings with multiple 16-bit char values per unicode text element).
        /// </summary>
        [TestMethod]
        public void EncodeAzureNameSurrogateSequence()
        {
            var surrogateSequence = char.ConvertFromUtf32(0x10380);
            var unencodedName = "a" + surrogateSequence;
            var expectedEncodedName = "a{0}D800{0}DF80".FormatInvariant(AzureNameEncoder.AzureEscapeCharacter);
            var encodedName = AzureNameEncoder.EncodeAzureName(unencodedName);
            this.AssertEncoding(expectedEncodedName, encodedName);
        }

        /// <summary>Null returns null.</summary>
        [TestMethod]
        public void EncodeXmlNameNull()
        {
            Assert.IsNull(AzureNameEncoder.EncodeXmlName(null));
        }

        /// <summary>Empty returns empty.</summary>
        [TestMethod]
        public void EncodeXmlNameEmpty()
        {
            this.AssertEncoding(string.Empty, AzureNameEncoder.EncodeXmlName(string.Empty));
        }

        /// <summary>Single char works.</summary>
        [TestMethod]
        public void EncodeXmlNameSingleChar()
        {
            this.AssertEncoding("a", AzureNameEncoder.EncodeXmlName("a"));
        }

        /// <summary>Encoding escape char gets double escaped.</summary>
        [TestMethod]
        public void EncodeXmlNameWithEscapeCharacter()
        {
            var unencodedName = "a{0}b".FormatInvariant(AzureNameEncoder.XmlEscapeCharacter);
            var expectedEncodedName = "a{0}{1:X4}b"
                .FormatInvariant(AzureNameEncoder.XmlEscapeCharacter, Convert.ToInt32(AzureNameEncoder.XmlEscapeCharacter));
            var encodedName = AzureNameEncoder.EncodeXmlName(unencodedName);
            this.AssertEncoding(expectedEncodedName, encodedName);
        }

        /// <summary>The encoded name is a valid xml element name.</summary>
        [TestMethod]
        public void EncodeXmlName()
        {
            // \u0b83 is not a valid first char but won't be encoded otherwise.
            // \u16a0 should get encoded regardless
            var unencodedName = "\u0b83\u0b83\u16a0";
            var expectedEncodedName = "{0}0B83\u0b83{0}16A0".FormatInvariant(AzureNameEncoder.XmlEscapeCharacter);

            try
            {
                // The unencoded name should cause an xml exception
                new XElement(unencodedName, "somevalue");
                Assert.Fail();
            }
            catch (XmlException)
            {
            }

            var encodedName = AzureNameEncoder.EncodeXmlName(unencodedName);

            // The encoded name should not cause an xml exception
            var element = new XElement(encodedName, "somevalue");
            this.AssertEncoding(expectedEncodedName, element.Name.ToString());
        }

        /// <summary>The Azure encoded name can be xml encoded correctly.</summary>
        [TestMethod]
        public void EncodeXmlNameFromAzureEncodedName()
        {
            var surrogateSequence = char.ConvertFromUtf32(0x10380);
            var unencodedName = "\u0b83\u0b83\u16a0aAzZ_09\u4e00\u0304 \u9fcf" + surrogateSequence;
            var expectedFullyEncodedName = "{0}0B83\u0b83{0}16A0aAzZ{1}005F09\u4e00{1}0304{1}0020{1}9FCF{1}D800{1}DF80"
                .FormatInvariant(AzureNameEncoder.XmlEscapeCharacter, AzureNameEncoder.AzureEscapeCharacter);
            
            var azureEncodedName = AzureNameEncoder.EncodeAzureName(unencodedName);

            try
            {
                // The azure only encoded name should cause an xml exception
                new XElement(azureEncodedName, "somevalue");
                Assert.Fail();
            }
            catch (XmlException)
            {
            }

            var xmlEncodedName = AzureNameEncoder.EncodeXmlName(azureEncodedName);

            // The encoded name should not cause an xml exception
            var element = new XElement(xmlEncodedName, "somevalue");
            this.AssertEncoding(expectedFullyEncodedName, element.Name.ToString());
        }

        /// <summary>
        /// Null returns null
        /// </summary>
        [TestMethod]
        public void UNEncodeNameNull()
        {
            Assert.IsNull(AzureNameEncoder.UnencodeName(null, AzureNameEncoder.AzureEscapeCharacter));
        }

        /// <summary>
        /// Empty returns empty
        /// </summary>
        [TestMethod]
        public void UNEncodeNameEmpty()
        {
            this.AssertEncoding(string.Empty, AzureNameEncoder.UnencodeName(string.Empty, AzureNameEncoder.AzureEscapeCharacter));
        }

        /// <summary>AlphaNumeric names with no special characters should not change.</summary>
        [TestMethod]
        public void UNEncodeNameNoEncodedChars()
        {
            var encodedName = "aAzZ09\u4e00";
            var unencodedName = AzureNameEncoder.UnencodeName(encodedName, AzureNameEncoder.AzureEscapeCharacter);
            this.AssertEncoding(encodedName, unencodedName);
        }

        /// <summary>Encoding escape char gets double escaped.</summary>
        [TestMethod]
        public void UNEncodeNameThatEscapesTheEscapeCharacter()
        {
            var expectedUnencodedName = "a{0}b".FormatInvariant(AzureNameEncoder.AzureEscapeCharacter);
            var encodedName = "a{0}{1:X4}b"
                .FormatInvariant(AzureNameEncoder.AzureEscapeCharacter, Convert.ToInt32(AzureNameEncoder.AzureEscapeCharacter));
            var unencodedName = AzureNameEncoder.UnencodeName(encodedName, AzureNameEncoder.AzureEscapeCharacter);
            this.AssertEncoding(expectedUnencodedName, unencodedName);
        }

        /// <summary>
        /// Surrogate sequences are encoded
        /// (.Net strings with multiple 16-bit char values per unicode text element).
        /// </summary>
        [TestMethod]
        public void UNEncodeAzureNameSurrogateSequence()
        {
            var surrogateSequence = char.ConvertFromUtf32(0x10380);
            var expectedUnencodedName = "a" + surrogateSequence;
            var encodedName = "a{0}D800{0}DF80".FormatInvariant(AzureNameEncoder.AzureEscapeCharacter);
            var unencodedName = AzureNameEncoder.UnencodeAzureName(encodedName);
            this.AssertEncoding(expectedUnencodedName, unencodedName);
        }

        /// <summary>
        /// The strings are identical on a character level after roundtripping through
        /// Azure and Xml encoding.
        /// </summary>
        [TestMethod]
        public void RoundtripFullyEncoded()
        {
            var surrogateSequence = char.ConvertFromUtf32(0x10380);
            var unencodedName = "\u0b83\u0b83\u16a0aAzZ_09\u4e00\u0304 \u9fcf" + surrogateSequence;
            var expectedFullyEncodedName = "{0}0B83\u0b83{0}16A0aAzZ{1}005F09\u4e00{1}0304{1}0020{1}9FCF{1}D800{1}DF80"
                .FormatInvariant(AzureNameEncoder.XmlEscapeCharacter, AzureNameEncoder.AzureEscapeCharacter);

            var azureEncodedName = AzureNameEncoder.EncodeAzureName(unencodedName);
            var xmlEncodedName = AzureNameEncoder.EncodeXmlName(azureEncodedName);
            var xmlRountTripName = AzureNameEncoder.UnencodeXmlName(xmlEncodedName);
            var roundtripName = AzureNameEncoder.UnencodeAzureName(xmlRountTripName);

            this.AssertEncoding(expectedFullyEncodedName, xmlEncodedName);
            this.AssertEncoding(unencodedName, roundtripName);
        }

        /// <summary>Ordinal string compare of two strings.</summary>
        /// <param name="s1">first string.</param>
        /// <param name="s2">second string.</param>
        private void AssertEncoding(string s1, string s2)
        {
            Assert.AreEqual(0, string.CompareOrdinal(s1, s2));
        }
    }
}
