// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Args.cs" company="Rare Crowds Inc">
// Copyright 2012-2013 Rare Crowds, Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using ConsoleAppUtilities;

namespace Utilities.Base64Codec
{
    /// <summary>Command-line arguments for Base64Codec</summary>
    public class Base64Args : CommandLineArguments
    {
        /// <summary>Gets or sets the input path</summary>
        [CommandLineArgument("-i", "Input file path.")]
        public FileInfo InFile { get; set; }

        /// <summary>Gets or sets the output path</summary>
        [CommandLineArgument("-o", "Output file path.")]
        public FileInfo OutFile { get; set; }

        /// <summary>Gets or sets a value indicating whether to encode</summary>
        [CommandLineArgument("-enc", "Encode from binary to base64.")]
        public bool Encode { get; set; }

        /// <summary>Gets or sets a value indicating whether to decode</summary>
        [CommandLineArgument("-dec", "Decode from base64 to binary.")]
        public bool Decode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the base64 data
        /// is wrapped in a DataContractSerializer XML envelope.
        /// </summary>
        [CommandLineArgument("-xmlser", "Base64 data wrapped in a DataContractSerializer XML envelope.")]
        public bool XmlSerialized { get; set; }

        /// <summary>Gets whether or not the arguments are valid</summary>
        public override bool ArgumentsValid
        {
            get
            {
                if (this.Encode == this.Decode)
                {
                    return false;
                }

                if (this.InFile == null || !this.InFile.Exists)
                {
                    return false;
                }

                if (this.OutFile == null)
                {
                    return false;
                }
                
                return true;
            }
        }
    }
}
