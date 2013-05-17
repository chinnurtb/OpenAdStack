//-----------------------------------------------------------------------
// <copyright file="HttpExtensionsFixture.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestUtilities;

namespace TestUtilitiesUnitTests
{
    /// <summary>
    /// Unit tests for the HttpExtensionsFixture
    /// </summary>
    [TestClass]
    public class HttpExtensionsFixture
    {
        /// <summary>Test recursively searching for values</summary>
        [TestMethod]
        public void TryGetValueRecursive()
        {
            var graph = new Dictionary<string, object>
            {
                {
                    "ThingsList",
                    new object[]
                    {
                        new Dictionary<string, object>
                        {
                            { "Name", "Foo" },
                            { "Value", 42 }
                        },
                        new Dictionary<string, object>
                        {
                            { "Name", "Bar" },
                            { "Value", 13 },
                            {
                                "SubThings",
                                new Dictionary<string, object>
                                {
                                    { "Name", "Xoo" },
                                    { "Value", 999 }
                                }
                            }
                        }
                    }
                },
                {
                    "AnotherThing",
                    new Dictionary<string, object>
                    {
                        { "Name", "Zoo" },
                        { "Value", 333 }
                    }
                }
            };

            Assert.IsTrue(HttpExtensions.GetValuesForKey(graph, "Name").Contains("Bar"));
            Assert.IsTrue(HttpExtensions.GetValuesForKey(graph, "Value").Contains(13));
            Assert.IsTrue(HttpExtensions.GetValuesForKey(graph, "Value").Contains(999));
            Assert.IsTrue(HttpExtensions.GetValuesForKey(graph, "Name").Contains("Zoo"));
        }

        /// <summary>Test following redirects using the test client and extensions</summary>
        [TestMethod]
        [TestCategory("NonBVT")]
        public void FollowRedirect()
        {
            var client = new RestTestClient("http://bit.ly");
            client.SendRequest(HttpMethod.GET, "NHjZLJ")
                .AssertIsValidRedirect()
                .FollowIfRedirect(client)
                .AssertStatusCode(HttpStatusCode.OK);
        }
    }
}
