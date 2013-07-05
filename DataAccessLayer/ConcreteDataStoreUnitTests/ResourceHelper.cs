// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ResourceHelper.cs" company="Rare Crowds Inc">
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
// <summary>
//   Helper class to load embedded resources
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Globalization;
using TestUtilities;

namespace ConcreteDataStoreUnitTests
{
    /// <summary>
    /// Helper class to load embedded resources
    /// </summary>
    public static class ResourceHelper
    {
        /// <summary>
        /// A specialization for this test assembly that loads an embedded xml resource
        /// scoped by the location of this type and the resource directory for this assembly.
        /// </summary>
        /// <param name="res">The resource name (without the directory).</param>
        /// <returns>The xml string.</returns>
        public static string LoadXmlResource(string res)
        {
            var resource = string.Format(CultureInfo.InvariantCulture, @"Resources.{0}", res);
            return EmbeddedResourceHelper.GetEmbeddedXmlAsString(typeof(ResourceHelper), resource);
        }
    }
}
