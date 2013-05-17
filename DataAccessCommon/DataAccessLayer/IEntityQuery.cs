// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IEntityQuery.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DataAccessLayer
{
    /// <summary>Interface to encapsulate entity related query string data.</summary>
    public interface IEntityQuery
    {
        /// <summary>Gets ad-hoc query values used when filtering an entity.</summary>
        Dictionary<string, string> QueryStringParams { get; }

        /// <summary>Checks whether query string params contain a given flag.</summary>
        /// <param name="flagValue">The flag to check.</param>
        /// <returns>True the flag is present.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1726", Justification = "Corresponds to Api usage.")]
        bool ContainsFlag(string flagValue);

        /// <summary>
        /// CheckPropertyRegexMatch walks the list of (top-level IEntity) properties and checks against
        /// a regex evaluator. If a property name match is found, a regex compare checks to see if the
        /// property value matches the regex. 
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>true if regex matches, false if no match </returns>
        bool CheckPropertyRegexMatch(IRawEntity entity);

        /// <summary>Clone this instance of IEntityQuery.</summary>
        /// <returns>A cloned instance of this IEntityQuery.</returns>
        IEntityQuery Clone();
    }
}
