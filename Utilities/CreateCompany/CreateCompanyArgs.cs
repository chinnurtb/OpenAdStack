// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CreateCompanyArgs.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using ConsoleAppUtilities;
using DataAccessLayer;

namespace Utilities.CreateCompany
{
    /// <summary>
    /// Command-line arguments for CreateUser
    /// </summary>
    public class CreateCompanyArgs : CommandLineArguments
    {
        /// <summary>Gets a value indicating whether the arguments are valid</summary>
        public override bool ArgumentsValid
        {
            // All arguments are optional defaults
            get { return true; }
        }

        /// <summary>Gets or sets the UserId for the DAL RequestContext</summary>
        [CommandLineArgument("-authid", "Authorization UserId to use when calling the DAL.")]
        public string AuthUserId { get; set; }

        /// <summary>Gets or sets the External Entity Id for the new company to create</summary>
        [CommandLineArgument("-eid", "ExternalEntityId of the company to create.")]
        public EntityId NewCompanyId { get; set; }

        /// <summary>Gets or sets the name for the new company to create</summary>
        [CommandLineArgument("-cn", "ExternalName of the company to create.")]
        public string NewCompanyName { get; set; }

        /// <summary>Gets or sets the connection string to the EntityRepository index</summary>
        [CommandLineArgument("-ics", "Connection string to the entity repository index. If omitted, the one in the app.config will be used.")]
        public string IndexConnectionString { get; set; }

        /// <summary>Gets or sets the connection string to the EntityRepository entity store</summary>
        [CommandLineArgument("-ecs", "Connection string to the entity repository index. If omitted, the one in the app.config will be used.")]
        public string EntityConnectionString { get; set; }

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
