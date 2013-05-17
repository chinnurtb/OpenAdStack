//-----------------------------------------------------------------------
// <copyright file="ActivityRequest.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;
using Utilities.Serialization;

namespace Activities
{
 /// <summary>
    /// Represents the result of an activity
    /// </summary>
    [DataContract]
    public class ActivityRequest : GenericXmlSerializableBase<ActivityRequest>
    {
        /// <summary>
        /// Unique id for the request
        /// </summary>
        private readonly string uid;

        /// <summary>
        /// Initializes a new instance of the ActivityRequest class.
        /// </summary>
        public ActivityRequest()
        {
            this.uid = Guid.NewGuid().ToString("N");
            this.Values = new Dictionary<string, string>();
            this.QueryValues = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets the unique id of this request
        /// </summary>
        public string Id
        {
            get { return "{0} ({1})".FormatInvariant(this.Task, this.uid); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the activity succeeded
        /// </summary>
        [DataMember]
        public string Task { get; set; }

        /// <summary>
        /// Gets the collection of values for the activity
        /// </summary>
        [DataMember]
        public Dictionary<string, string> Values { get; private set; }

        /// <summary>
        /// Gets the collection of query values for the activity
        /// </summary>
        /// <remarks>
        /// TODO: Remove. The Activity framework should NOT be aware of what "query values" are.
        /// </remarks>
        [DataMember]
        public Dictionary<string, string> QueryValues { get; private set; }

        /// <summary>
        /// Creates a new ActivityRequest instance from XML
        /// </summary>
        /// <param name="requestXml">The xml</param>
        /// <returns>The ActivityRequest instance</returns>
        public static ActivityRequest DeserializeFromXml(string requestXml)
        {
            return GenericXmlSerializableBase<ActivityRequest>.DeserializeFromXmlInternal(requestXml);
        }

        /// <summary>Attempts to get a value from the request</summary>
        /// <param name="valueName">Name of the value to get</param>
        /// <param name="value">The value</param>
        /// <returns>True if the value exists; otherwise, false.</returns>
        public bool TryGetValue(string valueName, out string value)
        {
            if (!this.Values.ContainsKey(valueName))
            {
                value = null;
                return false;
            }

            value = this.Values[valueName];
            return true;
        }

        /// <summary>Attempts to get an integer value from the request</summary>
        /// <param name="valueName">Name of the value to get</param>
        /// <param name="value">The value</param>
        /// <returns>
        /// True if the value exists and is a valid integer;
        /// otherwise, false.
        /// </returns>
        public bool TryGetIntegerValue(string valueName, out int value)
        {
            return TryGetAndParseValue<int>(
                valueName,
                s => Convert.ToInt32(s, CultureInfo.InvariantCulture),
                out value);
        }

        /// <summary>Attempts to get an decimal value from the request</summary>
        /// <param name="valueName">Name of the value to get</param>
        /// <param name="value">The value</param>
        /// <returns>
        /// True if the value exists and is a valid decimal;
        /// otherwise, false.
        /// </returns>
        public bool TryGetDecimalValue(string valueName, out decimal value)
        {
            return TryGetAndParseValue<decimal>(
                valueName,
                s => Convert.ToDecimal(s, CultureInfo.InvariantCulture),
                out value);
        }

        /// <summary>Attempts to get an enum value from the request</summary>
        /// <typeparam name="TEnum">Type of the enum</typeparam>
        /// <param name="valueName">Name of the value to get</param>
        /// <param name="value">The value</param>
        /// <returns>
        /// True if the value exists and is a valid enum value;
        /// otherwise, false.
        /// </returns>
        public bool TryGetEnumValue<TEnum>(string valueName, out TEnum value)
            where TEnum : struct
        {
            return TryGetAndParseValue<TEnum>(
                valueName,
                s =>
                    {
                        TEnum enumValue;
                        if (!Enum.TryParse(s, true, out enumValue))
                        {
                            throw new FormatException(
                                "Invalid {0} value: '{1}'"
                                .FormatInvariant(s, typeof(TEnum).FullName));
                        }

                        return enumValue;
                    },
                out value);
        }

        /// <summary>Attempts to get an decimal value from the request</summary>
        /// <typeparam name="TValue">Type of the value</typeparam>
        /// <param name="valueName">Name of the value to get</param>
        /// <param name="parse">Lambda used to parse the value</param>
        /// <param name="value">The value</param>
        /// <returns>
        /// True if the value exists and is well-formatted;
        /// otherwise, false.
        /// </returns>
        private bool TryGetAndParseValue<TValue>(
            string valueName,
            Func<string, TValue> parse,
            out TValue value)
        {
            value = default(TValue);

            string stringValue;
            if (!this.TryGetValue(valueName, out stringValue))
            {
                return false;
            }

            try
            {
                value = parse(stringValue);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
