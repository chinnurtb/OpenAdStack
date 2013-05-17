//-----------------------------------------------------------------------
// <copyright file="ODataAssociationValue.cs" company="Rare Crowds Inc.">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DataAccessLayer;
using Newtonsoft.Json;

namespace ConcreteDataStore
{
    /// <summary>Abstraction of an OData association value.</summary>
    internal class ODataAssociationValue
    {
        /// <summary>Lookup table of indexes into the odata association value.</summary>
        private readonly Dictionary<string, int> odataAssociationValueLU = new Dictionary<string, int>
            {
                { "TargetEntityId", 0 },
                { "TargetExternalType", 1 },
                { "ExternalName", 2 }
            };

        /// <summary>The delimiter for association value fields.</summary>
        private const string ODataValueDelimiter = "||";

        /// <summary>The delimiter between value groups in an association collection.</summary>
        private const string ODataValueCollectionDelimiter = "}{";

        /// <summary>The field strings.</summary>
        private readonly string[] valueFields;

        /// <summary>The raw odata value.</summary>
        private readonly string odataValueJson;

        /// <summary>Initializes a new instance of the <see cref="ODataAssociationValue"/> class.</summary>
        /// <param name="odataValue">The odata value.</param>
        public ODataAssociationValue(string odataValue)
        {
            this.odataValueJson = odataValue;

            // This is safe even if it is json. We won't be using this.valueFields in that scenario
            this.valueFields = odataValue.Split(new[] { ODataValueDelimiter }, StringSplitOptions.None);
        }

        /// <summary>Gets TargetEntityId.</summary>
        public EntityId TargetEntityId
        {
            get
            {
                var id = this.TryExtractLegacyValueField("TargetEntityId");
                return id != null ? new EntityId(id) : null;
            }
        }

        /// <summary>Gets TargetExternalType.</summary>
        public string TargetExternalType
        {
            get { return this.TryExtractLegacyValueField("TargetExternalType"); }
        }

        /// <summary>Gets ExternalName.</summary>
        public string ExternalName
        {
            get { return this.TryExtractLegacyValueField("ExternalName"); }
        }

        /// <summary>Gets TargetEntityIds.</summary>
        public EntityId[] TargetEntityIds
        {
            get
            {
                return this.TryExtractTargetIds();
            }
        }

        /// <summary>Serialize an association group target id list to an odata value.</summary>
        /// <param name="targetEntityIds">The target id's in the association group.</param>
        /// <returns>odata value.</returns>
        public static string SerializeAssociationGroup(EntityId[] targetEntityIds)
        {
            var idStrings = targetEntityIds.Select(id => id.ToString()).ToArray();
            return JsonConvert.SerializeObject(idStrings);
        }

        /// <summary>Split an odata value representing an association collection into individual association values.</summary>
        /// <param name="odataValue">The odata collection value.</param>
        /// <returns>A list of individual association values</returns>
        public static IList<string> SplitCollectionValues(string odataValue)
        {
            return odataValue.Split(new[] { ODataValueCollectionDelimiter }, StringSplitOptions.None).ToList();
        }

        /// <summary>Extract TargetIds from json in odata value.</summary>
        /// <returns>Id array or null if fail.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern.")]
        private EntityId[] TryExtractTargetIds()
        {
            try
            {
                return JsonConvert.DeserializeObject<string[]>(this.odataValueJson)
                    .Select(id => new EntityId(id)).ToArray();
            }
            catch (Exception)
            {
            }

            return null;
        }

        /// <summary>Extract fields by name from legacy associations.</summary>
        /// <param name="valueName">The name of the field.</param>
        /// <returns>The field value or null if fail.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern.")]
        private string TryExtractLegacyValueField(string valueName)
        {
            try
            {
                // There is no supported scenario where a legacy association could have only one field
                if (this.valueFields.Length <= 1)
                {
                    return null;
                }

                return this.valueFields[this.odataAssociationValueLU[valueName]];
            }
            catch (Exception)
            {
            }

            return null;
        }
    }
}
