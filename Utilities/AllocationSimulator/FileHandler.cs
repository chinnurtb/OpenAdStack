// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FileHandler.cs" company="Rare Crowds Inc">
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

namespace AllocationSimulator
{
    /// <summary>
    /// Encapsulate simple string-based file io.
    /// </summary>
    internal class FileHandler : IFileHandler
    {
        /// <summary>Write a string to a text file.</summary>
        /// <param name="fileName">File path.</param>
        /// <param name="textContent">String to write.</param>
        public void WriteFile(string fileName, string textContent)
        {
            using (var w = File.AppendText(fileName))
            {
                w.Write(textContent);
            }
        }

        /// <summary>Write a string to a text file.</summary>
        /// <param name="directoryName">Directory to create.</param>
        /// <returns>The fully qualified path that was created.</returns>
        public string CreateDirectory(string directoryName)
        {
            return Directory.CreateDirectory(directoryName).FullName;
        }
    }
}
