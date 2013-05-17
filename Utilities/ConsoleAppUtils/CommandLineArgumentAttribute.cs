// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommandLineArgumentAttribute.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ConsoleAppUtilities
{
    /// <summary>Attribute for decorating properties of CommandLineArguments as arguments</summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class CommandLineArgumentAttribute : Attribute
    {
        /// <summary>The strings that identify the argumentAttribute</summary>
        private string[] aliases;

        /// <summary>Initializes a new instance of the CommandLineArgumentAttribute class.</summary>
        /// <param name="identifier">Identifiers for the argument</param>
        /// <param name="description">Description of the argument</param>
        /// <param name="aliases">Optional aliases for the argument</param>
        public CommandLineArgumentAttribute(string identifier, string description, params string[] aliases)
        {
            this.Identifier = identifier;
            this.Description = description;
            this.aliases = aliases;
            this.AllIdentifiers = new[] { identifier }.Concat(this.aliases);
        }

        /// <summary>Gets or sets the default value for the argument</summary>
        public string DefaultValue { get; set; }

        /// <summary>Gets or sets the AppSetting to use as the default value</summary>
        public string DefaultAppSetting { get; set; }

        /// <summary>Gets the identifier for the argument</summary>
        public string Identifier { get; private set; }
        
        /// <summary>Gets the aliases for the argument</summary>
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Okay to suppress for attributes: http://msdn.microsoft.com/en-us/library/0fss9skc.aspx")]
        public string[] Aliases { get; private set; }

        /// <summary>Gets the description of the argument</summary>
        public string Description { get; private set; }

        /// <summary>Gets the identifier and aliases for the argument</summary>
        public IEnumerable<string> AllIdentifiers { get; private set; }

        /// <summary>Override equality to compare on GetHashCode</summary>
        /// <param name="obj">Object to compare to</param>
        /// <returns>True if hashcodes match, otherwise false</returns>
        public override bool Equals(object obj)
        {
            return obj != null && this.GetType() == obj.GetType() && this.GetHashCode() == obj.GetHashCode();
        }

        /// <summary>Gets a hashcode based upon the values of identifier, description and aliases</summary>
        /// <returns>The hashcode</returns>
        public override int GetHashCode()
        {
            return this.Identifier.GetHashCode() ^ this.Description.GetHashCode() ^ string.Join("|", this.aliases).GetHashCode();
        }
    }
}
