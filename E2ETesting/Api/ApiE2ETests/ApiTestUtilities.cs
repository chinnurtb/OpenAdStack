//-----------------------------------------------------------------------
// <copyright file="ApiTestUtilities.cs" company="Rare Crowds Inc">
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
