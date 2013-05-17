// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ResourceHelper.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
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
