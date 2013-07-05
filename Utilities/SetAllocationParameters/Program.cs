// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Rare Crowds Inc">
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
