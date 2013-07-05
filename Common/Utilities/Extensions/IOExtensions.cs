//-----------------------------------------------------------------------
// <copyright file="IOExtensions.cs" company="Rare Crowds Inc">
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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

/// <summary>Global scope IO related extensions</summary>
[SuppressMessage("Microsoft.Design", "CA1050", Justification = "Global by design")]
public static class IOExtensions
{
    /// <summary>Returns a deflated copy of a byte array</summary>
    /// <param name="this">The byte array</param>
    /// <returns>The deflated byte array</returns>
    public static byte[] Deflate(this byte[] @this)
    {
        using (var buffer = new MemoryStream())
        {
            using (var stream = new DeflateStream(buffer, CompressionMode.Compress))
            {
                stream.Write(@this, 0, @this.Length);
            }

            return buffer.ToArray();
        }
    }

    /// <summary>Returns an inflated copy of a byte array</summary>
    /// <param name="this">The deflated byte array</param>
    /// <returns>The inflated byte array</returns>
    public static byte[] Inflate(this byte[] @this)
    {
        using (var buffer = new MemoryStream())
        {
            using (var stream = new DeflateStream(new MemoryStream(@this), CompressionMode.Decompress))
            {
                while (true)
                {
                    var chunk = new byte[1024];
                    var read = stream.Read(chunk, 0, chunk.Length);
                    if (read == 0)
                    {
                        break;
                    }

                    buffer.Write(chunk, 0, read);
                }
            }

            return buffer.ToArray();
        }
    }
}
