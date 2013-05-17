// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EmbeddedResourceHelper.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Reflection;

namespace TestUtilities
{
    /// <summary>
    /// Helper class to load embedded resources
    /// </summary>
    public static class EmbeddedResourceHelper
    {
        /// <summary>
        /// Return an xml string from an embedded resource. Usage as follows:
        /// GetEmbeddedXmlAsString(typeof(MyTestClass), @"Resources.MyResource.xml")
        /// Must be called from the assembly containing the resource.
        /// </summary>
        /// <param name="scopingType">
        /// A type in the assembly containing the resource, usually just the calling
        /// test class. This is a little a bit of a cheater to make it easier to resolve. Usually the caller
        /// wraps this method in it's own specialiazation that provides the scopingType.
        /// </param>
        /// <param name="resourceName">
        /// The resource name including the folder the resource is located in. For example, if the
        /// resource call MyResource.xml is located in a folder called 'Resources' in the project,
        /// this would be @"Resources.MyResource.xml". Usually the caller wraps this method in it's
        /// own specialization that qualifies the resource name with it's folder as appropriate for
        /// the fixture.
        /// </param>
        /// <returns>Xml string.</returns>
        public static string GetEmbeddedXmlAsString(Type scopingType, string resourceName)
        {
            string result = string.Empty;

            // TODO: depend on the more general version once it starts using GetType().Assembly
            // instead of GetCallingAssembly
            var res = Assembly.GetCallingAssembly().GetManifestResourceStream(scopingType, resourceName);

            if (res != null)
            {
                result = new StreamReader(res).ReadToEnd();
            }

            return result;
        }

        /// <summary>
        /// Return a string from an embedded resource. Usage as follows:
        /// GetEmbeddedResourceAsString(typeof(MyTestClass), @"Resources.MyResource.foo")
        /// Must be called from the assembly containing the resource.
        /// </summary>
        /// <param name="scopingType">
        /// A type in the assembly containing the resource, usually just the calling
        /// test class. This is a little a bit of a cheater to make it easier to resolve. Usually the caller
        /// wraps this method in it's own specialiazation that provides the scopingType.
        /// </param>
        /// <param name="resourceName">
        /// The resource name including the folder the resource is located in. For example, if the
        /// resource call MyResource.foo is located in a folder called 'Resources' in the project,
        /// this would be @"Resources.MyResource.foo". Usually the caller wraps this method in it's
        /// own specialization that qualifies the resource name with it's folder as appropriate for
        /// the fixture.
        /// </param>
        /// <returns>Resource as string.</returns>
        public static string GetEmbeddedResourceAsString(Type scopingType, string resourceName)
        {
            string result = string.Empty;

            // TODO: this works fine for tests because the test assembly with the resource is the calling
            // assembly. However, if we want to make a production version of this helper 
            // we need to use something leverages obj.GetType().Assembly where 'obj' refers to an instance
            // of an object in the assy that has the resource
            var res = Assembly.GetCallingAssembly().GetManifestResourceStream(scopingType, resourceName);

            if (res != null)
            {
                result = new StreamReader(res).ReadToEnd();
            }

            return result;
        }

        /// <summary>
        /// Return a string from an embedded resource. Usage as follows:
        /// GetEmbeddedResourceAsString(typeof(MyTestClass), @"Resources.MyResource.foo")
        /// Must be called from the assembly containing the resource.
        /// </summary>
        /// <param name="scopingType">
        /// A type in the assembly containing the resource, usually just the calling
        /// test class. This is a little a bit of a cheater to make it easier to resolve. Usually the caller
        /// wraps this method in it's own specialiazation that provides the scopingType.
        /// </param>
        /// <param name="resourceName">
        /// The resource name including the folder the resource is located in. For example, if the
        /// resource call MyResource.foo is located in a folder called 'Resources' in the project,
        /// this would be @"Resources.MyResource.foo". Usually the caller wraps this method in it's
        /// own specialization that qualifies the resource name with it's folder as appropriate for
        /// the fixture.
        /// </param>
        /// <returns>Resource as an array of bytes.</returns>
        public static byte[] GetEmbeddedResourceAsByteArray(Type scopingType, string resourceName)
        {
            const int ChunkSize = 512;
            using (var outputBuffer = new MemoryStream())
            {
                using (var res = GetResourceStream(scopingType, resourceName))
                {
                    if (res == null)
                    {
                        return null;
                    }

                    var readBuffer = new byte[ChunkSize];
                    int read;
                    do
                    {
                        read = res.Read(readBuffer, 0, ChunkSize);
                        outputBuffer.Write(readBuffer, 0, read);
                    }
                    while (read > 0);
                }

                return outputBuffer.ToArray();
            }
        }

        /// <summary>
        /// Return a string from an embedded resource. Usage as follows:
        /// GetEmbeddedResourceAsString(typeof(MyTestClass), @"Resources.MyResource.foo")
        /// Must be called from the assembly containing the resource.
        /// </summary>
        /// <param name="scopingType">
        /// A type in the assembly containing the resource, usually just the calling
        /// test class. This is a little a bit of a cheater to make it easier to resolve. Usually the caller
        /// wraps this method in it's own specialiazation that provides the scopingType.
        /// </param>
        /// <param name="resourceName">
        /// The resource name including the folder the resource is located in. For example, if the
        /// resource call MyResource.foo is located in a folder called 'Resources' in the project,
        /// this would be @"Resources.MyResource.foo". Usually the caller wraps this method in it's
        /// own specialization that qualifies the resource name with it's folder as appropriate for
        /// the fixture.
        /// </param>
        /// <returns>Resource as a stream.</returns>
        private static Stream GetResourceStream(Type scopingType, string resourceName)
        {
            return scopingType.Assembly.GetManifestResourceStream(scopingType, resourceName);
        }
    }
}
