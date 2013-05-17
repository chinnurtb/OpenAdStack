// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConversionDictionary.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ConsoleAppUtilities
{
    /// <summary>
    /// Dictionary type which contains predicate matchers and functions that convert string arguments to their values
    /// </summary>
    [SuppressMessage("Microsoft.Usage", "CA2237", Justification = "Not for serialization")]
    public sealed class ConversionDictionary : Dictionary<Predicate<Type>, Func<Type, string, object>>
    {
        /// <summary>Initializes a new instance of the ConversionDictionary class</summary>
        public ConversionDictionary()
            : base()
        {
        }

        /// <summary>Initializes a new instance of the ConversionDictionary class</summary>
        /// <param name="dictionary">The ConversionDictionary whose elements are copied to the new ConversionDictionary</param>
        public ConversionDictionary(ConversionDictionary dictionary)
            : base(dictionary)
        {
        }
    }
}