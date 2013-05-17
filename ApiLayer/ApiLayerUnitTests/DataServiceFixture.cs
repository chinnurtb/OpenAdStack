// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataServiceFixture.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using Activities;
using ApiLayer;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Queuing;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;
using WorkItems;

namespace ApiLayerUnitTests
{
    /// <summary>Test fixture for the data service</summary>
    [TestClass]
    public class DataServiceFixture : ServiceBase
    {
        /// <summary>Builds the response from the activity result.</summary>
        /// <remarks>This is the only place from which the response needs to be built.</remarks>
        /// <param name="result">Result returned from the activity</param>
        /// <returns>Stream that contains the json response to be returned</returns>
        protected override Stream BuildResponse(ActivityResult result)
        {
            throw new NotImplementedException();
        }
    }
}
