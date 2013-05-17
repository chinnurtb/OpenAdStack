// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace SetAllocationParameters
{
    /// <summary>
    /// Program to set custom dynamic allocation parameters on an entity.
    /// </summary>
    public static class Program
    {
        /// <summary>SetAllocParams entry point</summary>
        /// <param name="args">The args.</param>
        /// <returns>0 if successful</returns>
        public static int Main(string[] args)
        {
            try
            {
                var worker = WorkHandlerFactory.BuildWorkHandler(args);
                worker.Run();
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
                return 2;
            }
        }
    }
}
