// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WorkerRole.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
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
