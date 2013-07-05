// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TestUtilities.cs" company="Rare Crowds Inc">
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
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using ApiLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Utilities.IdentityFederation;

namespace ApiLayerUnitTests
{
    /// <summary>Assembly-wide test setup</summary>
    [TestClass]
    public static class TestUtilities
    {
        /// <summary>Test value for the name identifier claim</summary>
        public static readonly string NameIdentifierClaimValue = Guid.NewGuid().ToString();

        /// <summary>Assembly-wide test initialization</summary>
        /// <param name="context">Not used.</param>
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            Trace.WriteLine("AssemblyInit " + context.TestName);

            var mockClaimRetriever = MockRepository.GenerateMock<IClaimRetriever>();
            mockClaimRetriever.Stub(f =>
                f.GetClaimValue(Arg<string>.Is.Equal(ServiceBase.NameIdentifierClaim)))
                .Return(NameIdentifierClaimValue);
            mockClaimRetriever.Stub(f =>
                f.GetClaimValue(Arg<string>.Is.Equal(EntityService.NameIdentifierClaim)))
                .Return(NameIdentifierClaimValue);

            ServiceBase.ClaimRetriever = mockClaimRetriever;
            EntityService.ClaimRetriever = mockClaimRetriever;
        }
    }
}
