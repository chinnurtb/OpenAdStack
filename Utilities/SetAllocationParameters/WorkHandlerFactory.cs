// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WorkHandlerFactory.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using ConsoleAppUtilities;
using DataAccessLayer;
using Diagnostics;
using Microsoft.Practices.Unity;
using RuntimeIoc.WorkerRole;

namespace SetAllocationParameters
{
    /// <summary>Factory for WorkHandler</summary>
    public static class WorkHandlerFactory
    {
        /// <summary>Build a new instance of the WorkHandler.</summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>The work handler instance.</returns>
        public static WorkHandler BuildWorkHandler(string[] args)
        {
            var arguments = CommandLineArguments.Create<SetAllocationParametersArgs>(args);
            if (!arguments.ArgumentsValid)
            {
                throw new InvalidOperationException();
            }

            var paramsJson = File.ReadAllText(arguments.ParamsFile.FullName);
            var replace = arguments.Replace;

            // If target not specified assume the company is the target
            var companyEntityId = arguments.CompanyEntityId;
            var targetEntityId = arguments.TargetEntityId ?? companyEntityId;

            var logFile = arguments.LogFile != null 
                ? arguments.LogFile.FullName 
                : ".\\setallocationparameterslog.txt";
            LogManager.Initialize(new[] { new FileLogger(logFile) });

            var repository = RuntimeIocContainer.Instance.Resolve<IEntityRepository>();

            return BuildWorkHandler(repository, paramsJson, companyEntityId, targetEntityId, replace);
        }

        /// <summary>Build a new instance of the WorkHandler.</summary>
        /// <param name="repository">An IEntityRepository instance.</param>
        /// <param name="paramsJson">Allocation parameters json.</param>
        /// <param name="companyEntityId">Company entity id.</param>
        /// <param name="targetEntityId">Target entity id.</param>
        /// <param name="replace">True to destructively overwrite params.</param>
        /// <returns>The work handler instance.</returns>
        public static WorkHandler BuildWorkHandler(
            IEntityRepository repository, 
            string paramsJson, 
            EntityId companyEntityId, 
            EntityId targetEntityId, 
            bool replace)
        {
            return new WorkHandler(repository, paramsJson, companyEntityId, targetEntityId, replace);
        }
    }
}
