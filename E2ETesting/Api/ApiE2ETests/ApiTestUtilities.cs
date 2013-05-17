//-----------------------------------------------------------------------
// <copyright file="ApiTestUtilities.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Configuration;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestUtilities;

namespace E2ETestUtilities
{
    /// <summary>Utilities for API tests</summary>
    [TestClass]
    public class ApiTestUtilities
    {
        /// <summary>Starts the compute emulator</summary>
        /// <param name="context">Not used.</param>
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            Trace.Write(context);
            E2EEmulatorUtilities.StartEmulators();
        }

        /// <summary>Stops the compute emulator</summary>
        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            E2EEmulatorUtilities.StopEmulators();
        }
    }
}
