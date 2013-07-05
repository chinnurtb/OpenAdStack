// -----------------------------------------------------------------------
// <copyright file="StringConversions.cs" company="Rare Crowds Inc">
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
// -----------------------------------------------------------------------

using System;
using System.Globalization;

namespace DataAccessLayer
{
    /// <summary>
    /// Static methods for string/native conversion.
    /// </summary>
    internal static class StringConversions
    {
        /// <summary>To string is degenerate case.</summary>
        /// <param name="value">The string serialized value.</param>
        /// <returns>Native string.</returns>
        public static string StringToNativeString(string value)
        {
            return value;
        }

        /// <summary>From string is degenerate case.</summary>
        /// <param name="value">The native value.</param>
        /// <returns>String serialized value.</returns>
        public static string NativeStringToString(string value)
        {
            return value;
        }

        /// <summary>String to int.</summary>
        /// <param name="value">The string serialized value.</param>
        /// <returns>Native int.</returns>
        public static int StringToNativeInt32(string value)
        {
            int num;
            if (!int.TryParse(value, out num))
            {
                throw new ArgumentException("Not a valid Int32: {0}".FormatInvariant(value));
            }

            return num;
        }

        /// <summary>Int to string.</summary>
        /// <param name="value">The native value.</param>
        /// <returns>String serialized value.</returns>
        public static string NativeInt32ToString(int value)
        {
            return value.ToString("D", CultureInfo.InvariantCulture);
        }

        /// <summary>String to long.</summary>
        /// <param name="value">The string serialized value.</param>
        /// <returns>Native long.</returns>
        public static long StringToNativeInt64(string value)
        {
            long num;
            if (!long.TryParse(value, out num))
            {
                throw new ArgumentException("Not a valid Int64: {0}".FormatInvariant(value));
            }

            return num;
        }

        /// <summary>Long to string.</summary>
        /// <param name="value">The native value.</param>
        /// <returns>String serialized value.</returns>
        public static string NativeInt64ToString(long value)
        {
            return value.ToString("D", CultureInfo.InvariantCulture);
        }

        /// <summary>String to double.</summary>
        /// <param name="value">The string serialized value.</param>
        /// <returns>Native double.</returns>
        public static double StringToNativeDouble(string value)
        {
            double num;

            if (!double.TryParse(value, out num))
            {
                throw new ArgumentException("Not a valid Double: {0}".FormatInvariant(value));
            }

            return num;
        }

        /// <summary>Double to string.</summary>
        /// <param name="value">The native value.</param>
        /// <returns>String serialized value.</returns>
        public static string NativeDoubleToString(double value)
        {
            // Use the 'round-trip' format specifier to make sure rounding errors
            // don't force it out of range.
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        /// <summary>String to DateTime.</summary>
        /// <param name="value">The string serialized value.</param>
        /// <returns>Native DateTime.</returns>
        public static DateTime StringToNativeDateTime(string value)
        {
            // Use DateTimeStyles.RoundtripKind to correspond to the 'o' format specifier when serialization to string.
            DateTime dateValue;
            if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out dateValue))
            {
                throw new ArgumentException("Not a valid DateTime: {0}".FormatInvariant(value));
            }

            return dateValue;
        }

        /// <summary>DateTime to string.</summary>
        /// <param name="value">The native value.</param>
        /// <returns>String serialized value.</returns>
        public static string NativeDateTimeToString(DateTime value)
        {
            // Convert to ISO 8601 (yyyy-MM-ddTHH:mm:ss.fffffffZ) where Z indicates UTC.
            // We will always serialize as UTC.
            var utcValue = value.ToUniversalTime();
            var dateString = utcValue.ToString("o", CultureInfo.InvariantCulture);

            return dateString;
        }

        /// <summary>String to bool.</summary>
        /// <param name="value">The string serialized value.</param>
        /// <returns>Native bool.</returns>
        public static bool StringToNativeBool(string value)
        {
            bool flag;
            if (!bool.TryParse(value, out flag))
            {
                throw new ArgumentException("Not a valid Boolean: {0}".FormatInvariant(value));
            }

            return flag;
        }

        /// <summary>bool to string.</summary>
        /// <param name="value">The native value.</param>
        /// <returns>String serialized value.</returns>
        public static string NativeBoolToString(bool value)
        {
            // Remarkably, Azure chokes on mixed-case string value
            return value.ToString(CultureInfo.InvariantCulture).ToLowerInvariant();
        }

        /// <summary>String to byte[].</summary>
        /// <param name="value">The string serialized value.</param>
        /// <returns>Native byte[].</returns>
        public static byte[] StringToNativeByteArray(string value)
        {
            byte[] arr;

            try
            {
                arr = Convert.FromBase64String(value);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Not a valid Base64 string: {0}".FormatInvariant(value), ex);
            }

            return arr;
        }

        /// <summary>byte[] to string.</summary>
        /// <param name="value">The native value.</param>
        /// <returns>String serialized value.</returns>
        public static string NativeByteArrayToString(byte[] value)
        {
            return Convert.ToBase64String(value);
        }

        /// <summary>String to Guid.</summary>
        /// <param name="value">The string serialized value.</param>
        /// <returns>Native Guid.</returns>
        public static Guid StringToNativeGuid(string value)
        {
            Guid guidValue;
            if (!Guid.TryParse(value, out guidValue))
            {
                throw new ArgumentException("Not a valid Guid format: {0}".FormatInvariant(value));
            }

            return guidValue;
        }

        /// <summary>Guid to string.</summary>
        /// <param name="value">The native value.</param>
        /// <returns>String serialized value.</returns>
        public static string NativeGuidToString(Guid value)
        {
            return value.ToString("N", CultureInfo.InvariantCulture);
        }
    }
}
