//Copyright 2012-2013 Rare Crowds, Inc.
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
