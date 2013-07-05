// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WorkerRole.cs" company="Rare Crowds Inc">
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

using System.Diagnostics;
using System.Net;
using System.Threading;
using AllocationSimulator;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace SimulatorRole
{
    /// <summary>Placeholder worker role for simulator</summary>
    public class WorkerRole : RoleEntryPoint
    {
        /// <summary>Role entry point</summary>
        public override void Run()
        {
            // This is a sample worker implementation. Replace with your logic.
            Trace.WriteLine("SimulatorRole entry point called. Placeholder for " + typeof(Simulator).FullName, "Information");

            while (true)
            {
                Thread.Sleep(10000);
                Trace.WriteLine("Working", "Information");
            }
        }

        /// <summary>Role initialization</summary>
        /// <returns>True if initialization succeeds, False if it fails.</returns>
        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;
            return base.OnStart();
        }
    }
}
