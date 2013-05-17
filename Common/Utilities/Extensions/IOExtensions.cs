//-----------------------------------------------------------------------
// <copyright file="IOExtensions.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
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
