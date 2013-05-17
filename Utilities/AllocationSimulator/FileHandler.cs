// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FileHandler.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
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
