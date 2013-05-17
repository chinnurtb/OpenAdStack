// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IFileHandler.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.IO;

namespace AllocationSimulator
{
    /// <summary>
    /// Interface definition for simple string-based file io.
    /// </summary>
    public interface IFileHandler
    {
        /// <summary>Write a string to a text file.</summary>
        /// <param name="fileName">File path.</param>
        /// <param name="textContent">String to write.</param>
        void WriteFile(string fileName, string textContent);

        /// <summary>Write a string to a text file.</summary>
        /// <param name="directoryName">Directory to create.</param>
        /// <returns>The fully qualified path that was created.</returns>
        string CreateDirectory(string directoryName);
    }
}