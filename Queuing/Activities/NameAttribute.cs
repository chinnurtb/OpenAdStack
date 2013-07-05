// -----------------------------------------------------------------------
// <copyright file="NameAttribute.cs" company="Rare Crowds Inc">
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

namespace Activities
{
    /// <summary>
    /// Attribute specifying the name of the activity
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class NameAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the NameAttribute class.
        /// </summary>
        /// <param name="value">The name</param>
        public NameAttribute(string value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets the name of the activity
        /// </summary>
        public string Value { get; private set; }
    }
}
