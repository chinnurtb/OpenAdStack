// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using ConsoleAppUtilities;

namespace Utilities.Base64Codec
{
    /// <summary>Contains the program entry point</summary>
    public static class Program
    {
        /// <summary>Separator for console output</summary>
        private const string Separator = "--------------------------------------------------------------------------------";

        /// <summary>Format for usage</summary>
        private const string UsageFormat =
@"Encodes/decodes files to/from base64.
Usage: Base64.exe {{-i ""InputFileName""}} {{-o ""OutputFileName""}} {{-enc|-dec}} [-xmlser]
{0}";
        
        /// <summary>Serializer for XML wrapped base64 data</summary>
        private static readonly DataContractSerializer Serializer = new DataContractSerializer(typeof(byte[]));

        /// <summary>Gets usage information</summary>
        private static string Usage
        {
            get
            {
                return UsageFormat
                    .FormatInvariant(CommandLineArguments.GetDescriptions<Base64Args>());
            }
        }

        /// <summary>Program entry point</summary>
        /// <param name="args">Command-line arguments</param>
        /// <returns>The error code</returns>
        internal static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(Usage);
                return 0;
            }

            try
            {
                var arguments = CommandLineArguments.Create<Base64Args>(args);
                if (!arguments.ArgumentsValid)
                {
                    Console.WriteLine(Usage);
                    return 1;
                }

                if (arguments.Encode)
                {
                    var bytes = File.ReadAllBytes(arguments.InFile.FullName);
                    var base64 = arguments.XmlSerialized ?
                        SerializeToBase64(bytes) :
                        Convert.ToBase64String(bytes);
                    File.WriteAllText(arguments.OutFile.FullName, base64);
                    Console.WriteLine(
                        @"Wrote {0} base64 encoded bytes to ""{1}""."
                        .FormatInvariant(bytes.Length, arguments.OutFile.FullName));
                }
                else if (arguments.Decode)
                {
                    var base64 = File.ReadAllText(arguments.InFile.FullName);
                    var bytes = arguments.XmlSerialized ?
                        DeserializeFromBase64(base64) :
                        Convert.FromBase64String(base64);
                    File.WriteAllBytes(arguments.OutFile.FullName, bytes);
                    Console.WriteLine(
                        @"Read {0} base64 encoded bytes from ""{1}""."
                        .FormatInvariant(bytes.Length, arguments.InFile.FullName));
                }

                return 0;
            }
            catch (ArgumentException ae)
            {
                Console.WriteLine("Invalid argument(s)");
                Console.WriteLine(Usage);
                Console.Error.WriteLine(Separator);
                Console.Error.WriteLine(ae);
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 2;
            }
        }

        /// <summary>Serialize bytes to a base64 string</summary>
        /// <param name="bytes">The bytes</param>
        /// <returns>The base64 string</returns>
        private static string SerializeToBase64(byte[] bytes)
        {
            using (var writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                using (var xmlWriter = new XmlTextWriter(writer))
                {
                    Serializer.WriteObject(xmlWriter, bytes);
                }

                return writer.ToString();
            }            
        }

        /// <summary>Deserialize a base64 string to bytes</summary>
        /// <param name="base64">The base64 string</param>
        /// <returns>The bytes</returns>
        private static byte[] DeserializeFromBase64(string base64)
        {
            using (var reader = new StringReader(base64))
            {
                using (var xmlReader = new XmlTextReader(reader))
                {
                    return (byte[])Serializer.ReadObject(xmlReader);
                }
            }
        }
    }
}
