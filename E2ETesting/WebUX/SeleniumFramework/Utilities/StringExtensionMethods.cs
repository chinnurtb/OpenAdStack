//-----------------------------------------------------------------------
// <copyright file="StringExtensionMethods.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Globalization;

namespace SeleniumFramework.Utilities
{
    /// <summary>
    /// This class contains extension methods on string type
    /// </summary>
    public static class StringExtensionMethods
    {
        #region Methods

        /// <summary>
        /// Convert any possible string value of a given enumeration type to its internal representation
        /// </summary>
        /// <typeparam name="T">Enumeration type</typeparam>
        /// <param name="value">String value</param>
        /// <returns>Enum type</returns>
        public static T ToEnum<T>(this string value)
        {
            return (T)Enum.Parse(typeof(T), value);
        }

        /// <summary>
        /// Returns the string after removing spaces
        /// </summary>
        /// <param name="value">Extension method on string value</param>
        /// <returns>Returns string</returns>
        public static string RemoveSpaces(this string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                return value.Trim();
            }

            return string.Empty;
        }

        /// <summary>
        /// Converts the Object into String
        /// </summary>
        /// <param name="value">Extension method on object type</param>
        /// <returns>Returns string</returns>
        public static string String(this object value)
        {
            if (value == null)
            {
                return string.Empty;
            }
            else
            {
                return Convert.ToString(value, CultureInfo.CurrentCulture).RemoveSpaces();
            }
        }

        #endregion
    }
}
