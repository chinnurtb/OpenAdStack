//-----------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Rare Crowds Inc">
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