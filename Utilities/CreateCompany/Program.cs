// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ConsoleAppUtilities;
using DataAccessLayer;
using EntityUtilities;
using Microsoft.Practices.Unity;
using RuntimeIoc.WorkerRole;

namespace Utilities.CreateCompany
{
    /// <summary>Contains the program entry point</summary>
    public static class Program
    {
        /// <summary>Gets usage information</summary>
        private static string Usage
        {
            get
            {
                return
                    "Creates a new company and saves it directly to the Entity Repository.\n" +
                    @"Usage: CreateCompany.exe -authid ""Authorization UserId"" [optional arguments]" +
                    CommandLineArguments.GetDescriptions<CreateCompanyArgs>();
            }
        }

        /// <summary>Program entry point</summary>
        /// <param name="args">Command-line arguments</param>
        /// <returns>The error code</returns>
        internal static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(Usage);
                return 0;
            }

            try
            {
                var arguments = CommandLineArguments.Create<CreateCompanyArgs>(args);

                // Override the connection strings if provided on the command-line
                if (!string.IsNullOrWhiteSpace(arguments.IndexConnectionString))
                {
                    ConfigurationManager.AppSettings["Index.ConnectionString"] = arguments.IndexConnectionString;
                }

                if (!string.IsNullOrWhiteSpace(arguments.EntityConnectionString))
                {
                    ConfigurationManager.AppSettings["Entity.ConnectionString"] = arguments.EntityConnectionString;
                }

                // Use the IEntityRepository from WorkerRole.RuntimeIoc to create the company
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Creating company..."));
                var repository = RuntimeIocContainer.Instance.Resolve<IEntityRepository>();
                Console.WriteLine(CreateCompany(arguments, repository));

                return 0;
            }
            catch (ArgumentException ae)
            {
                Console.WriteLine(ae);
                Console.WriteLine();
                Console.WriteLine(Usage);
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unhandled exception:");
                Console.WriteLine(ex.ToString());
                return 2;
            }
        }

        /// <summary>Creates a new company</summary>
        /// <param name="args">Arguments containing information for creating the company</param>
        /// <param name="repository">Entity repository in which to create the company</param>
        /// <returns>A link to the API for verifying the company.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "command-line utility code - catch all is fine")]
        private static string CreateCompany(CreateCompanyArgs args, IEntityRepository repository)
        {
            // If a company id is not provided assume a default of 0000000000000000000000000000001
            var companyIdToCreate = args.NewCompanyId ?? 1;
            var companyNameToCreate = !string.IsNullOrWhiteSpace(args.NewCompanyName) ? args.NewCompanyName : "DefaultCompany";

            var context = new RequestContext
            {
                UserId = !string.IsNullOrWhiteSpace(args.AuthUserId) ? args.AuthUserId : string.Empty,
                ExternalCompanyId = companyIdToCreate
            };

            // See if it already exists - throw is ambiguous but expected with with a clean store. Be
            // optimistic in this case.
            try
            {
                var companies = repository.GetEntitiesById(context, new[] { companyIdToCreate });
                if (companies.Count == 1)
                {
                    return string.Format(CultureInfo.InvariantCulture, "Company {0} already exists.", (string)companyIdToCreate);
                }
            }
            catch
            {
            }

            // Otherwise we need to create it
            var companyJson = string.Format(
                CultureInfo.InvariantCulture,
                @"{{""EntityCategory"":""{0}"",""ExternalName"":""{1}""}}",
                CompanyEntity.CompanyEntityCategory,
                companyNameToCreate);

            var companyEntity = EntityJsonSerializer.DeserializeCompanyEntity(companyIdToCreate, companyJson);

            try
            {
                repository.AddCompany(context, companyEntity);
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(System.Data.SqlClient.SqlException) &&
                    ex.Message.StartsWith("Login failed", StringComparison.Ordinal))
                {
                    // Most likely cause of login failing is not having mixed mode authentication enabled
                    Console.WriteLine("\n!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    Console.WriteLine("!! SQL Login failed. Please verify the database has Sql Authentication enabled. !!");
                    Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n");
                }

                // If we get here and then it probably means the company exists in the index but 
                // not in the entity store - bad system state.
                Console.WriteLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "Create Company Failed. Index store and entity store may not be in sync.:\n{0}\n\n",
                    companyEntity.SerializeToJson()));
                throw;
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "Created Company:\n{0}",
                companyEntity.SerializeToJson());
        }
    }
}
