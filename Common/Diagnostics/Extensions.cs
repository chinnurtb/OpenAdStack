//-----------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System.IO;

namespace Diagnostics
{
    /// <summary>Extensions useful for diagnostics</summary>
    internal static class Extensions
    {
        /// <summary>
        /// Gets just the name of the FileInfo without its extension
        /// </summary>
        /// <param name="this">The FileInfo</param>
        /// <returns>The file name</returns>
        public static string GetFileName(this FileInfo @this)
        {
            return @this.Name.Substring(0, @this.Name.Length - @this.Extension.Length);
        }
    }
}
