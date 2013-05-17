// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CreateUserArgs.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using ConsoleAppUtilities;
using DataAccessLayer;

namespace Utilities.CreateUser
{
    /// <summary>
    /// Command-line arguments for CreateUser
    /// </summary>
    public class CreateUserArgs : CommandLineArguments
    {
        /// <summary>Default API domain</summary>
        private const string DefaultApiDomain = "rcprotoapnxapp.cloudapp.net";

        /// <summary>Initializes a new instance of the CreateUserArgs class.</summary>
        public CreateUserArgs()
        {
            // Default values for arguments
            this.UserId = Guid.NewGuid().ToString("N");
            this.ApiDomain = DefaultApiDomain;
        }

        /// <summary>Gets or sets the ContactEmail for the new User</summary>
        [CommandLineArgument("-e", "Contact email address for the user")]
        public string UserEmailAddress { get; set; }

        /// <summary>
        /// Gets or sets the temporary UserId for the new User
        /// </summary>
        [CommandLineArgument("-uid", "Specify a UserId. If omitted a new GUID will be used as a temporary Id.")]
        public string UserId { get; set; }

        /// <summary>Gets or sets the UserId for the DAL RequestContext</summary>
        [CommandLineArgument("-authid", "Authorization UserId to use when calling the DAL.")]
        public string AuthUserId { get; set; }

        /// <summary>Gets or sets the AccessDescriptor.</summary>
        [CommandLineArgument("-acc", "Access Descriptor to give the user.")]
        public string AccessDescriptor { get; set; }

        /// <summary>Gets or sets the CompanyId for the DAL RequestContext</summary>
        [CommandLineArgument("-cid", "EntityId of the company in the context of which to create the user (optional).")]
        public EntityId CompanyId { get; set; }

        /// <summary>Gets or sets the domain where the API is deployed</summary>
        [CommandLineArgument("-d", @"Domain of the API deployment to be used in the verification link. If omitted, ""RCProtoApnxApp.cloudapp.net"" will be used.")]
        public string ApiDomain { get; set; }

        /// <summary>Gets or sets the connection string to the EntityRepository index</summary>
        [CommandLineArgument("-ics", "Connection string to the entity repository index. If omitted, the one in the app.config will be used.")]
        public string IndexConnectionString { get; set; }

        /// <summary>Gets or sets the connection string to the EntityRepository entity store</summary>
        [CommandLineArgument("-ecs", "Connection string to the entity repository index. If omitted, the one in the app.config will be used.")]
        public string EntityConnectionString { get; set; }

        /// <summary>Gets whether or not the arguments are valid</summary>
        public override bool ArgumentsValid
        {
            get { return !string.IsNullOrWhiteSpace(this.UserEmailAddress); }
        }

        /// <summary>Gets the conversions for parsing the arguments</summary>
        protected override ConversionDictionary Conversions
        {
            get
            {
                // TODO: Find a cleaner way to do this. IEnumerable.Concat doesn't work.
                var conversions = new ConversionDictionary
                {
                    { t => t.IsAssignableFrom(typeof(EntityId)), (t, s) => new EntityId(s) }
                };
                
                foreach (var c in base.Conversions)
                {
                    conversions.Add(c.Key, c.Value);
                }

                return conversions;
            }
        }
    }
}
