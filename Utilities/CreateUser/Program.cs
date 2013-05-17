// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using ConsoleAppUtilities;
using DataAccessLayer;
using EntityUtilities;
using Microsoft.Practices.Unity;
using RuntimeIoc.WebRole;

namespace Utilities.CreateUser
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
                    "Creates a new user and saves it directly to the Entity Repository.\n" +
                    @"Usage: CreateUser.exe -e ""user@email.com"" -authid ""Authorization UserId"" [optional arguments]" +
                    CommandLineArguments.GetDescriptions<CreateUserArgs>();
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
                var arguments = CommandLineArguments.Create<CreateUserArgs>(args);

                // Override the connection strings if provided on the command-line
                if (!string.IsNullOrWhiteSpace(arguments.IndexConnectionString))
                {
                    ConfigurationManager.AppSettings["Index.ConnectionString"] = arguments.IndexConnectionString;
                }

                if (!string.IsNullOrWhiteSpace(arguments.EntityConnectionString))
                {
                    ConfigurationManager.AppSettings["Entity.ConnectionString"] = arguments.EntityConnectionString;
                }

                // Use the IEntityRepository from WorkerRole.RuntimeIoc to get/create the user
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Creating user {0} ...", arguments.UserEmailAddress));
                var repository = RuntimeIocContainer.Instance.Resolve<IEntityRepository>();

                var user = TryGetUser(arguments, repository) ?? CreateUser(arguments, repository);

                // Use the IUserAccessRepository from WorkerRole.RuntimeIoc to add user access
                Console.WriteLine("Adding user access ...");
                var userAccessRepository = RuntimeIocContainer.Instance.Resolve<IUserAccessRepository>();
                AddUserAccess(arguments, user.ExternalEntityId, userAccessRepository);
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

            return 0;
        }

        /// <summary>Get a user by user id if it exists</summary>
        /// <param name="args">Arguments containing information for creating the user</param>
        /// <param name="repository">Entity repository</param>
        /// <returns>The user entity.</returns>
        private static UserEntity TryGetUser(CreateUserArgs args, IEntityRepository repository)
        {
            UserEntity user = null;

            try
            {
                user = repository.GetUser(new RequestContext(), args.UserId);
                Console.WriteLine("User {0} exists already. Skipping Create.", args.UserId);
            }
            catch (ArgumentException)
            {
            }

            return user;
        }

        /// <summary>Creates a new user</summary>
        /// <param name="args">Arguments containing information for creating the user</param>
        /// <param name="repository">Entity repository in which to create the user</param>
        /// <returns>The user entity.</returns>
        private static UserEntity CreateUser(CreateUserArgs args, IEntityRepository repository)
        {
            var minimumUserJson = string.Format(
                CultureInfo.InvariantCulture,
                @"{{""EntityCategory"":""User"",""Properties"":{{""ContactEmail"":""{0}"",""UserId"":""{1}""}}}}",
                args.UserEmailAddress,
                args.UserId);

            var user = EntityJsonSerializer.DeserializeUserEntity(new EntityId(), minimumUserJson);

            var context = new RequestContext
            {
                UserId = !string.IsNullOrWhiteSpace(args.AuthUserId) ? args.AuthUserId : string.Empty,
                ExternalCompanyId = args.CompanyId
            };

            try
            {
                repository.SaveUser(context, user);
                Console.WriteLine(
                    string.Format(
                    CultureInfo.InvariantCulture,
                    "Created User:\n{0}",
                    user.SerializeToJson()));
            }
            catch (Exception ex)
            {
                if (ex is SqlException && ex.Message.StartsWith("Login failed", StringComparison.Ordinal))
                {
                    // Most likely cause of login failing is not having mixed mode authentication enabled
                    Console.WriteLine("\n!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    Console.WriteLine("!! SQL Login failed. Please verify the database has Sql Authentication enabled. !!");
                    Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n");
                }

                Console.WriteLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "Create User Failed. Index store and entity store may not be in sync.:\n{0}\n\n",
                    user.SerializeToJson()));
                throw;
            }

            return user;
        }

        /// <summary>Creates a new user</summary>
        /// <param name="args">Arguments containing information for creating the user</param>
        /// <param name="userEntityId">Entity Id of user</param>
        /// <param name="userAccessRepository">User access repository for user</param>
        private static void AddUserAccess(CreateUserArgs args, EntityId userEntityId, IUserAccessRepository userAccessRepository)
        {
            if (string.IsNullOrWhiteSpace(args.AccessDescriptor))
            {
                Console.WriteLine("No Access Descriptor specified. Skipping.");
                return;
            }

            var accessList = new List<string> { args.AccessDescriptor };
            var result = userAccessRepository.AddUserAccessList(userEntityId, accessList);

            if (!result)
            {
                Console.WriteLine("Access Descriptor could not be added. See diagnostic logs.");
                return;
            }

            Console.WriteLine(
                string.Format(
                CultureInfo.InvariantCulture,
                "Access Descriptor: {0} added for user entity Id: {1}",
                args.AccessDescriptor,
                (string)userEntityId));
        }
    }
}
