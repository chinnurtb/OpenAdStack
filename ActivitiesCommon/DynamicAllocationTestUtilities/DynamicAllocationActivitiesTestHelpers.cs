// -----------------------------------------------------------------------
// <copyright file="DynamicAllocationActivitiesTestHelpers.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Linq;
using System.Reflection;
using DataAccessLayer;
using DynamicAllocation;
using DynamicAllocationUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace DynamicAllocationTestUtilities
{
    /// <summary>Test helpers for DynamicAllocation related activity UnitTests</summary>
    public static class DynamicAllocationActivitiesTestHelpers
    {
        /// <summary>MeasureSourceFactory stub setup.</summary>
        public static void SetupMeasureSourceFactoryStub()
        {
            var embeddedMeasureSource = new EmbeddedJsonMeasureSource(
                Assembly.GetExecutingAssembly(), "DynamicAllocationTestUtilities.Resources.MeasureMap.js");

            var mockMeasureSourceProvider = MockRepository.GenerateMock<IMeasureSourceProvider>();
            mockMeasureSourceProvider.Stub(f => f.Version).Return(0);
            mockMeasureSourceProvider.Stub(f => f.DeliveryNetwork).Return(DeliveryNetworkDesignation.AppNexus);
            mockMeasureSourceProvider.Stub(f => f.GetMeasureSources(Arg<object[]>.Is.Anything)).Return(null).WhenCalled(
                call =>
                    {
                        var args = (object[])call.Arguments[0];
                        var companyEntity = args.OfType<CompanyEntity>().Single();
                        var campaignEntity = args.OfType<CampaignEntity>().Single();
                        var sources = new[]
                            {
                                embeddedMeasureSource, 
                                companyEntity.GetMeasureSource(), 
                                campaignEntity.GetMeasureSource()
                            };
                        call.ReturnValue = sources.Where(source => source != null);
                    });

            MeasureSourceFactory.Initialize(new[] { mockMeasureSourceProvider });
        }
    }
}
