// -----------------------------------------------------------------------
// <copyright file="SourceNameAttribute.cs" company="Rare Crowds Inc">
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
using System.Text.RegularExpressions;

namespace ScheduledActivities
{
    /// <summary>Attribute specifying the name of the scheduled activity source</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class SourceNameAttribute : Attribute
    {
        /// <summary>Initializes a new instance of the SourceNameAttribute class.</summary>
        /// <param name="value">The name</param>
        public SourceNameAttribute(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException("value");
            }

            this.Value = value.ToLowerInvariant();
        }

        /// <summary>Gets the name</summary>
        public string Value { get; private set; }
    }
}
