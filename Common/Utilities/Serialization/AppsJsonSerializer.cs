// -----------------------------------------------------------------------
// <copyright file="AppsJsonSerializer.cs" company="Rare Crowds Inc">
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
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Utilities.Serialization
{
    /// <summary>Wrapper for underlying Json serialization library.</summary>
    public static class AppsJsonSerializer
    {
        /// <summary>Serialize an object to JSON.</summary>
        /// <param name="instance">The target object.</param>
        /// <returns>A JSON string.</returns>
        public static string SerializeObject(object instance)
        {
            return HandleError(() => JsonConvert.SerializeObject(instance, new DateTimeJsonConverter()));
        }

        /// <summary>Deserialize an object from JSON.</summary>
        /// <param name="json">The source JSON.</param>
        /// <typeparam name="T">The target type.</typeparam>
        /// <returns>The deserialized object.</returns>
        public static T DeserializeObject<T>(string json)
        {
            return HandleError(() => JsonConvert.DeserializeObject<T>(json, new DateTimeJsonConverter()));
        }

        /// <summary>Deserialize an anonymous type from JSON and an object of the target type.</summary>
        /// <param name="json">The source JSON.</param>
        /// <param name="anonymousTypeDef">An object of the target type to use as a template.</param>
        /// <typeparam name="T">Anonymous type.</typeparam>
        /// <returns>An object of the anonymous type.</returns>
        public static T DeserializeAnonymousType<T>(string json, T anonymousTypeDef)
        {
            return HandleError(() => JsonConvert.DeserializeAnonymousType(json, anonymousTypeDef));
        }

        /// <summary>Exception handling wrapper method.</summary>
        /// <typeparam name="T">Return type of operation being guarded.</typeparam>
        /// <param name="operation">The operation to perform.</param>
        /// <returns>The return value of the operation.</returns>
        private static T HandleError<T>(Func<T> operation)
        {
            try
            {
                return operation();
            }
            catch (JsonReaderException e)
            {
                throw new AppsJsonException("Could not deserialize json to target type.", e);
            }
            catch (Exception e)
            {
                throw new AppsJsonException("Unexpected de/serialization error.", e);
            }
        }
        
        /// <summary>
        /// Custom DateTime Json converter. Handles multiple incoming json date formats for
        /// backward compatibility with previously serialized objects.
        /// </summary>
        private class DateTimeJsonConverter : DateTimeConverterBase
        {
            /// <summary>
            /// Writes the JSON representation of the object.
            /// </summary>
            /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter"/> to write to.</param><param name="value">The value.</param><param name="serializer">The calling serializer.</param>
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var converter = new IsoDateTimeConverter();
                converter.WriteJson(writer, value, serializer);
            }

            /// <summary>
            /// Reads the JSON representation of the object.
            /// </summary>
            /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader"/> to read from.</param><param name="objectType">Type of the object.</param><param name="existingValue">The existing value of object being read.</param><param name="serializer">The calling serializer.</param>
            /// <returns>
            /// The object value.
            /// </returns>
            [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern.")]
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.Value is DateTime)
                {
                    return reader.Value;
                }

                try
                {
                    var convertIso = new IsoDateTimeConverter();
                    return convertIso.ReadJson(reader, objectType, existingValue, serializer);
                }
                catch (Exception)
                {
                }

                var convertJs = new JavaScriptDateTimeConverter();
                return convertJs.ReadJson(reader, objectType, existingValue, serializer);
            }
        }
    }
}
