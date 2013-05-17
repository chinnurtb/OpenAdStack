//-----------------------------------------------------------------------
// <copyright file="MathExtensions.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;

/// <summary>Global scope text related extensions</summary>
[SuppressMessage("Microsoft.Design", "CA1050", Justification = "Global by design")]
[CLSCompliant(false)]
public static class MathExtensions
{
    /// <summary>Counts the number of set bits in a 32-bit integer</summary>
    /// <remarks>From MIT HAKMEM</remarks>
    /// <param name="value">The value</param>
    /// <returns>Number of set bits</returns>
    public static int CountSetBits(this uint value)
    {
        uint count = value
            - ((value >> 1) & 0xDB6DB6DB)
            - ((value >> 2) & 0x49249249);
        return (int)
            (((count + (count >> 3))
            & 0xC71C71C7) % 63);
    }
}
