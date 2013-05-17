// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RepositoryEntityQuery.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace DataAccessLayer
{
    /// <summary>
    /// Implementation of IEntityQuery for Repository calls. This is not currently used
    /// and is a stub implementation.
    /// </summary>
    public class RepositoryEntityQuery : IEntityQuery
    {
        /// <summary>Initializes a new instance of the <see cref="RepositoryEntityQuery"/> class.</summary>
        public RepositoryEntityQuery() : this(null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="RepositoryEntityQuery"/> class.</summary>
        /// <param name="queryStringParams">A dictionary of query string parameters.</param>
        public RepositoryEntityQuery(Dictionary<string, string> queryStringParams)
        {
            this.QueryStringParams = queryStringParams ?? new Dictionary<string, string>();
        }

        /// <summary>Gets ad-hoc query values used when filtering an entity.</summary>
        public Dictionary<string, string> QueryStringParams { get; private set; }

        /// <summary>Checks whether query string params contain a given flag.</summary>
        /// <param name="flagValue">The flag to check.</param>
        /// <returns>True the flag is present.</returns>
        public bool ContainsFlag(string flagValue)
        {
            return false;
        }

        /// <summary>
        /// CheckPropertyRegexMatch walks the list of (top-level IEntity) properties and checks against
        /// a regex evaluator. If a property name match is found, a regex compare checks to see if the
        /// property value matches the regex. 
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>true if regex matches, false if no match </returns>
        public bool CheckPropertyRegexMatch(IRawEntity entity)
        {
            return false;
        }

        /// <summary>Clone this instance of IEntityQuery.</summary>
        /// <returns>A cloned instance of this IEntityQuery.</returns>
        public IEntityQuery Clone()
        {
            var clonedQueryStringParameters = this.QueryStringParams
                .ToDictionary(queryStringParam => queryStringParam.Key, queryStringParam => queryStringParam.Value);

            return new RepositoryEntityQuery(clonedQueryStringParameters);
        }
    }
}
