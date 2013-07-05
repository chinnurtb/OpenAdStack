//-----------------------------------------------------------------------
// <copyright file="CanonicalResourceFixture.cs" company="Rare Crowds Inc.">
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ResourceAccess;

namespace ResourceAccessUnitTests
{
    /// <summary>
    /// Fixture to test CanonicalResource and CanonicalResourceFactory
    /// </summary>
    [TestClass]
    public class CanonicalResourceFixture
    {
        /// <summary>resource uri is not a well-formed absolute uri.</summary>
        [TestMethod]
        public void BuildCanonicalResourceInvalidUri()
        {
            Assert.IsNull(CanonicalResource.BuildCanonicalResource("notavalidabsoluteuri", "dontcare"));
            Assert.IsNull(CanonicalResource.BuildCanonicalResource("notavalidabsoluteuri", "dontcare"));
        }

        /// <summary>resource uri is not a valid action.</summary>
        [TestMethod]
        public void BuildCanonicalResourceInvalidAction()
        {
            Assert.IsNull(CanonicalResource.BuildCanonicalResource("http://localhost/foo", string.Empty));
            Assert.IsNull(CanonicalResource.BuildCanonicalResource("http://localhost/foo", string.Empty));
        }

        /// <summary>Happy path.</summary>
        [TestMethod]
        public void BuildCanonicalResourceSuccess()
        {
            Assert.IsNotNull(CanonicalResource.BuildCanonicalResource("http://localhost/foo", "GET"));
            Assert.IsNotNull(CanonicalResource.BuildCanonicalResource("http://localhost/foo", "GET"));
        }

        /// <summary>Determine if this is an api uri.</summary>
        [TestMethod]
        public void CanonicalDescriptionIsApiResource()
        {
            Assert.IsTrue(this.BuildCanonicalResource("http://localhost/api/company", "GET").IsApiResource);
            Assert.IsFalse(this.BuildCanonicalResource("http://localhost/company", "GET").IsApiResource);
        }
        
        /// <summary>Get the canonical descriptor for a namespace uri.</summary>
        [TestMethod]
        public void CanonicalDescriptionNamespace()
        {
            var resource = this.BuildCanonicalResource("http://localhost/api/company", "GET");
            Assert.AreEqual("COMPANY:*:#:GET", resource.CanonicalDescriptor);
        }

        /// <summary>Get the canonical descriptor for an specific entity id.</summary>
        [TestMethod]
        public void CanonicalDescriptionEntityId()
        {
            var resource = this.BuildCanonicalResource("http://localhost/api/company/00000000000000000000000000000001", "GET");
            var resource2 = this.BuildCanonicalResource("http://localhost/api/company/00000000-0000-0000-0000-000000000001", "GET");
            Assert.AreEqual("COMPANY:00000000000000000000000000000001:#:GET", resource.CanonicalDescriptor);
            Assert.AreEqual("COMPANY:00000000000000000000000000000001:#:GET", resource2.CanonicalDescriptor);
        }

        /// <summary>Get the canonical descriptor for a subnamespace.</summary>
        [TestMethod]
        public void CanonicalDescriptionSubNamespace()
        {
            var resource = this.BuildCanonicalResource("http://localhost/api/company/00000000000000000000000000000001/campaign", "GET");
            Assert.AreEqual("COMPANY:00000000000000000000000000000001:CAMPAIGN:*:#:GET", resource.CanonicalDescriptor);
        }

        /// <summary>Get the canonical descriptor for a subnamespace with entity id.</summary>
        [TestMethod]
        public void CanonicalDescriptionSubNamespaceEntityId()
        {
            var resource = this.BuildCanonicalResource("http://localhost/api/company/00000000000000000000000000000001/campaign/00000000000000000000000000000002", "GET");
            Assert.AreEqual("COMPANY:00000000000000000000000000000001:CAMPAIGN:00000000000000000000000000000002:#:GET", resource.CanonicalDescriptor);
        }

        /// <summary>Get canonical descriptor with a message on the query string.</summary>
        [TestMethod]
        public void CanonicalDescriptorMessage()
        {
            var resource = this.BuildCanonicalResource("http://localhost/api/company?message=bar", "GET");
            Assert.AreEqual("COMPANY:*:#:GET:BAR", resource.CanonicalDescriptor);
        }

        /// <summary>Additional query string parameters are benign.</summary>
        [TestMethod]
        public void CanonicalDescriptorAdditionalQueryBenign()
        {
            var resource = this.BuildCanonicalResource("http://localhost/api/company?message=bar&messagefoo", "GET");
            Assert.AreEqual("COMPANY:*:#:GET:BAR", resource.CanonicalDescriptor);
        }

        /// <summary>Get canonical descriptor with a message on the query string but no parameter value.</summary>
        [TestMethod]
        public void CanonicalDescriptorNoParameterValueOk()
        {
            var resource = this.BuildCanonicalResource("http://localhost/api/company?queryfoo", "GET");
            Assert.AreEqual("COMPANY:*:#:GET:QUERYFOO", resource.CanonicalDescriptor);
        }

        /// <summary>Get canonical descriptor with a query string no parameters.</summary>
        [TestMethod]
        public void CanonicalDescriptorMessageNoParameterName()
        {
            var resource = this.BuildCanonicalResource("http://localhost/api/company?", "GET");
            Assert.AreEqual("COMPANY:*:#:GET", resource.CanonicalDescriptor);
        }

        /// <summary>IsApiResource should return false for non-api uri.</summary>
        [TestMethod]
        public void IsApiResourceFalse()
        {
            var resource = this.BuildCanonicalResource("http://localhost/css/foo", "GET");
            Assert.IsFalse(resource.IsApiResource);
        }

        /// <summary>Get the canonical descriptor when a file specified in path.</summary>
        [TestMethod]
        public void CanonicalDescriptionWebFile()
        {
            var resource = this.BuildCanonicalResource("http://localhost/css/foo.html", "GET");
            Assert.AreEqual("CSS:FOO.HTML:#:GET", resource.CanonicalDescriptor);
        }

        /// <summary>Get the canonical descriptor when a directory is the first item in the path.</summary>
        [TestMethod]
        public void CanonicalDescriptionWebDirectory()
        {
            var resource = this.BuildCanonicalResource("http://localhost/css/foo", "GET");
            Assert.AreEqual("CSS:FOO:*:#:GET", resource.CanonicalDescriptor);
        }

        /// <summary>Get the canonical descriptor when path is arbitrarily long.</summary>
        [TestMethod]
        public void CanonicalDescriptionWebTruncateLongerThanRecognized()
        {
            var resource = this.BuildCanonicalResource("http://localhost/css/foo/foo/foo/foo/foo", "GET");
            Assert.AreEqual("CSS:FOO:FOO:FOO:FOO:FOO:*:#:GET", resource.CanonicalDescriptor);
        }

        /// <summary>Get the canonical descriptor for a root path.</summary>
        [TestMethod]
        public void CanonicalDescriptionWebRoot()
        {
            var resource = this.BuildCanonicalResource("http://localhost/", "GET");
            Assert.AreEqual("ROOT:#:GET", resource.CanonicalDescriptor);
        }

        /// <summary>Get query strings on web resources are benign.</summary>
        [TestMethod]
        public void CanonicalDescriptionWebQueryBenign()
        {
            var resource = this.BuildCanonicalResource("http://localhost/css?foo", "GET");
            Assert.AreEqual("CSS:*:#:GET:FOO", resource.CanonicalDescriptor);
        }

        /// <summary>Build a canonical resource given a uri string and an action.</summary>
        /// <param name="uriString">The uri string.</param>
        /// <param name="action">The action.</param>
        /// <returns>CanonicalResource object.</returns>
        private CanonicalResource BuildCanonicalResource(string uriString, string action)
        {
            return new CanonicalResource(new Uri(uriString, UriKind.Absolute), action);
        }
    }
}
