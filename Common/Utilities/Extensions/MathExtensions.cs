//-----------------------------------------------------------------------
// <copyright file="MathExtensions.cs" company="Rare Crowds Inc">
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
