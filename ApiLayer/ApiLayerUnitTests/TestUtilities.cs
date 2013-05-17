// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TestUtilities.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
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
