//-----------------------------------------------------------------------
// <copyright file="QueryRecord.cs" company="Rare Crowds Inc.">
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

using System.Collections.Generic;
using System.Linq;
using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>
    /// Helper class to access fields in a query record.
    /// </summary>
    public class QueryRecord
    {
        /// <summary>The fields of the record.</summary>
        private readonly IDictionary<string, object> recordFields;

        /// <summary>Initializes a new instance of the <see cref="QueryRecord"/> class.</summary>
        /// <param name="record">The dictionary containing the record fields.</param>
        public QueryRecord(IDictionary<string, object> record)
        {
            this.recordFields = record;
        }

        /// <summary>Determine if the record has a value for the field.</summary>
        /// <param name="name">The name of the field.</param>
        /// <returns>True if the field is present and not null.</returns>
        public bool HasField(string name)
        {
            return this.recordFields.ContainsKey(name) && this.recordFields[name] != null;
        }

        /// <summary>Get a value from the result sets.</summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="name">The name of the value.</param>
        /// <returns>The value.</returns>
        public T GetValue<T>(string name)
        {
            if (!this.recordFields.ContainsKey(name))
            {
                throw new DataAccessException("Unexpected Sql Result. {0} not found in query result.".FormatInvariant(name));
            }

            var value = this.recordFields[name];

            if (value == null && default(T) == null)
            {
                return default(T);
            }

            if (value == null)
            {
                throw new DataAccessException("Unexpected Sql Result. {0} cannot be null.".FormatInvariant(name));
            }

            if (value.GetType() != typeof(T))
            {
                throw new DataAccessException("Unexpected Sql Result. {0} is not of type {1}.".FormatInvariant(name, typeof(T).FullName));
            }

            return (T)value;
        }

        /// <summary>Determine if there is a matching field in the record.</summary>
        /// <typeparam name="T">Type of the value to match.</typeparam>
        /// <param name="fieldToMatch">Name of the field to match.</param>
        /// <param name="valueToMatch">Value to match.</param>
        /// <returns>True if there is a match.</returns>
        public bool Match<T>(string fieldToMatch, T valueToMatch)
        {
            return this.recordFields.Any(f => f.Key == fieldToMatch && f.Value.Equals(valueToMatch));
        }
    }
}