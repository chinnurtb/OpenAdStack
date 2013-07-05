//-----------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Rare Crowds Inc">
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
