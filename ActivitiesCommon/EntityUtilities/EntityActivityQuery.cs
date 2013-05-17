// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EntityActivityQuery.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using DataAccessLayer;

namespace EntityUtilities
{
    /// <summary>Encapsulation of query string parameter access used with entity activities.</summary>
    public class EntityActivityQuery : IEntityQuery
    {
        /// <summary>Initializes a new instance of the <see cref="EntityActivityQuery"/> class.</summary>
        public EntityActivityQuery() : this(null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="EntityActivityQuery"/> class.</summary>
        /// <param name="queryStringParams">A dictionary of query string parameters.</param>
        public EntityActivityQuery(Dictionary<string, string> queryStringParams)
        {
            this.QueryStringParams = queryStringParams ?? new Dictionary<string, string>();
        }

        /// <summary>Gets ad-hoc query values used when filtering an entity.</summary>
        public Dictionary<string, string> QueryStringParams { get; private set; }

        /// <summary>Checks whether query string params contain a given flag.</summary>
        /// <param name="flagValue">The flag to check.</param>
        /// <returns>True the flag is present.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1726", Justification = "Corresponds to Api usage.")]
        public bool ContainsFlag(string flagValue)
        {
            var flagsToQuery = this.QueryStringParams
                .Where(kvp => kvp.Key.ToUpperInvariant() == "FLAGS").ToList();

            // TODO: The test is a bit loose.
            var flags = flagsToQuery.Any() ? flagsToQuery.First().Value.ToUpperInvariant() : null;
            return !string.IsNullOrWhiteSpace(flags) && flags.Contains(flagValue.ToUpperInvariant());
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
            // Default will be to treat properties as matched if there are no filters
            var queryValues = this.QueryStringParams;
            if (queryValues == null)
            {
                return true;
            }

            // TODO: Generalize, this only works for top-level IEntity properties and is narrow
            // with regard to casing
            IEnumerable<string> propertyNames =
                typeof(IRawEntity).GetProperties().Where(p => p.PropertyType == typeof(EntityProperty)).Select(p => p.Name);

            foreach (var name in propertyNames)
            {
                var entityProperty = (EntityProperty)typeof(IRawEntity).GetProperty(name).GetValue(entity, null);
                if (entityProperty != null)
                {
                    if (queryValues.Count > 0)
                    {
                        // perform regex checking on this property
                        foreach (string key in queryValues.Keys)
                        {
                            if (key == entityProperty.Name.ToLower(CultureInfo.InvariantCulture))
                            {
                                if (Regex.IsMatch(entityProperty.Value.SerializationValue, queryValues[key]) == false)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            // we are here if there is no match for the queryValue key to property name, retrun default of true
            return true;
        }

        /// <summary>Clone this instance of IEntityQuery.</summary>
        /// <returns>A cloned instance of this IEntityQuery.</returns>
        public IEntityQuery Clone()
        {
            var clonedQueryStringParameters = this.QueryStringParams
                .ToDictionary(queryStringParam => queryStringParam.Key, queryStringParam => queryStringParam.Value);

            return new EntityActivityQuery(clonedQueryStringParameters);
        }
    }
}