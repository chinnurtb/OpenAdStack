//-----------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Emerging Media Group">
//      Copyright Emerging Media Group. All rights reserved.
// </copyright>
// <remarks>
//      You may find some additional information about this class at https://github.com/robdmoore/NQUnit and https://github.com/Sqdw/SQUnit.
// </remarks>
//-----------------------------------------------------------------------

using System;
using System.Xml.Linq;

namespace TestUtilities
{
    /// <summary>
    /// Helper class for validating Qunit test results
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Provides a case-insensitive comparison between an XName element and a string.
        /// </summary>
        /// <param name="elmName">The XName to compare</param>
        /// <param name="name">The string to compare</param>
        /// <returns>Whether or not the XName and the string are the same (case-insensitive)</returns>
        public static bool Is(this XName elmName, string name)
        {
            return elmName.ToString().Equals(name, StringComparison.OrdinalIgnoreCase);
        }
    }
}