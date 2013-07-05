//-----------------------------------------------------------------------
// <copyright file="ResultValuesAttribute.cs" company="Rare Crowds Inc">
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Activities
{
    /// <summary>
    /// Attribute specifying what values are required in the ActivityRequest
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class ResultValuesAttribute : Attribute
    {
        /// <summary>
        /// List of the names of the required values
        /// </summary>
        private string[] valueNames;

        /// <summary>
        /// Initializes a new instance of the ResultValuesAttribute class.
        /// </summary>
        /// <param name="valueNames">Names of the required values</param>
        public ResultValuesAttribute(params string[] valueNames)
        {
            this.valueNames = valueNames;
        }

        /// <summary>
        /// Gets the names of the required values
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Okay to suppress for attributes: http://msdn.microsoft.com/en-us/library/0fss9skc.aspx")]
        public string[] ValueNames
        {
            get
            {
                return this.valueNames.ToArray();
            }
        }
    }
}
