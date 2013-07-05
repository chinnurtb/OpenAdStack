//-----------------------------------------------------------------------
// <copyright file="TextExtensions.cs" company="Rare Crowds Inc">
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

/// <summary>Global scope text related extensions</summary>
[SuppressMessage("Microsoft.Design", "CA1050", Justification = "Global by design")]
public static class TextExtensions
{
    /// <summary>
    /// Replaces the format item(s) in a specified string with the string representation
    /// corresponding objects in a specified array.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <returns>
    /// A copy of format in which the format items have been replaced by the string
    /// representation of the corresponding objects in args.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">format is null</exception>
    /// <exception cref="System.FormatException">
    /// format is invalid.-or- The index of a format item is less than zero, or greater
    /// than or equal to the length of the args array.
    /// </exception>
    public static string FormatInvariant(this string format, params object[] args)
    {
        return string.Format(CultureInfo.InvariantCulture, format, args ?? new object[0]);
    }

    /// <summary>
    /// Gets the left <paramref name="length"/> characters of a string.
    /// If length is longer than the string, the original string is returned.
    /// </summary>
    /// <param name="this">The string.</param>
    /// <param name="length">Length of the left substring to get.</param>
    /// <returns>The left substring.</returns>
    public static string Left(this string @this, int length)
    {
        return @this.Substring(0, Math.Min(length, @this.Length));
    }

    /// <summary>
    /// Gets the right <paramref name="length"/> characters of a string.
    /// If length is longer than the string, the original string is returned.
    /// </summary>
    /// <param name="this">The string.</param>
    /// <param name="length">Length of the right substring to get.</param>
    /// <returns>The right substring.</returns>
    public static string Right(this string @this, int length)
    {
        var chars = Math.Min(length, @this.Length);
        var offset = @this.Length - chars;
        return @this.Substring(offset, chars);
    }

    /// <summary>Returns a string representation of a dictionary</summary>
    /// <typeparam name="TKey">Key type</typeparam>
    /// <typeparam name="TValue">Value type</typeparam>
    /// <param name="this">The dictionary</param>
    /// <returns>The string representation</returns>
    public static string ToString<TKey, TValue>(this IDictionary<TKey, TValue> @this)
    {
        var elements = new List<string>();
        foreach (var kvp in @this)
        {
            elements.Add("{0}={1}".FormatInvariant(kvp.Key, kvp.Value));
        }

        return elements.Count > 0 ?
            "[\n\t{0}\n]".FormatInvariant(string.Join(",\n\t", elements)) :
            "[]";
    }
}
