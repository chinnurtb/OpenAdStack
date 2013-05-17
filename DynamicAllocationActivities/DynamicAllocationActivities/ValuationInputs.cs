//-----------------------------------------------------------------------
// <copyright file="ValuationInputs.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DataAccessLayer;
using DynamicAllocation;
using Utilities.Serialization;

using daName = DynamicAllocationUtilities.DynamicAllocationEntityProperties;

namespace DynamicAllocationActivities
{
    /// <summary>
    /// Class to encapsulate valuation inputs serialization and convert to a CampaignDefinition for consumption by.
    /// dynamic allocation.
    /// </summary>
    internal class ValuationInputs
    {
        /// <summary>Initializes a new instance of the <see cref="ValuationInputs"/> class.</summary>
        /// <param name="measureInputs">The measure inputs.</param>
        /// <param name="nodeOverrides">The node overrides.</param>
        internal ValuationInputs(MeasureSetsInput measureInputs, IDictionary<MeasureSet, decimal> nodeOverrides)
        {
            this.MeasureSetsInput = measureInputs;
            this.NodeOverrides = nodeOverrides;

            // This will allow cached valuations to be created from legacy inputs but will get overwritten
            // the first time inputs are changed.
            this.ValuationInputsFingerprint = "legacyfingerprint";
        }

        /// <summary>Initializes a new instance of the <see cref="ValuationInputs"/> class.</summary>
        /// <param name="measureInputsJson">The measure inputs json.</param>
        /// <param name="nodeOverridesJson">The node overrides json.</param>
        internal ValuationInputs(
            string measureInputsJson, string nodeOverridesJson)
        {
            this.MeasureSetsInput = DeserializeMeasuresJson(measureInputsJson);
            this.NodeOverrides = string.IsNullOrEmpty(nodeOverridesJson) ? null
                : this.NodeOverrides = DeserializeNodeOverridesJson(nodeOverridesJson);
            this.ValuationInputsFingerprint = ComputeFingerprint(measureInputsJson, nodeOverridesJson);
        }

        /// <summary>Gets the Node overrides</summary>
        internal IDictionary<MeasureSet, decimal> NodeOverrides { get; private set; }

        /// <summary>
        /// Gets an enumerable of the MeasuresInputs
        /// a measuresinput contains the measure, the OR grouping for the measure, and a bool for if it's pinned
        /// </summary>
        internal MeasureSetsInput MeasureSetsInput { get; private set; }

        /// <summary>Gets the fingerprint of the valuation inputs.</summary>
        internal string ValuationInputsFingerprint { get; private set; }

        /// <summary>Deserialize measures Json.</summary>
        /// <param name="measuresJson">Measures Json.</param>
        /// <returns>The deserialized MeasureSetInput.</returns>
        internal static MeasureSetsInput DeserializeMeasuresJson(string measuresJson)
        {
            var measuresSerializable = AppsJsonSerializer.DeserializeObject<MeasureSetsInputSerialized>(measuresJson);
            
            return new MeasureSetsInput
            {
                IdealValuation = measuresSerializable.IdealValuation,
                MaxValuation = measuresSerializable.MaxValuation,
                Measures = measuresSerializable.Measures.Select(m =>
                    new MeasuresInput
                    {
                        Measure = Convert.ToInt64(m.measureId, CultureInfo.InvariantCulture),
                        Valuation = m.valuation,
                        Group = m.@group,
                        Pinned = m.pinned
                    })
            };
        }

        /// <summary>Deserialize node valuations Json.</summary>
        /// <param name="nodeValuationSetsJson">Node Valuation Set Json </param>
        /// <returns>The deserialized node valuation set.</returns>
        internal static Dictionary<MeasureSet, decimal> DeserializeNodeOverridesJson(string nodeValuationSetsJson)
        {
            // TODO: IdealValuation is currently dropped on the floor until DA can consume it
            var nodeValuationSetSerializable = AppsJsonSerializer
                .DeserializeObject<List<NodeValuationSerializationElement>>(nodeValuationSetsJson);

            if (nodeValuationSetSerializable == null)
            {
                return null;
            }

            return nodeValuationSetSerializable.ToDictionary(
                e => new MeasureSet(e.MeasureSet.Select(m => Convert.ToInt64(m, CultureInfo.InvariantCulture))),
                e => (decimal)e.MaxValuation);
        }

        /// <summary>
        /// Serialize Measures Json
        /// </summary>
        /// <param name="valuationSet">nodeValuationSet to serialize</param>
        /// <returns>JSON string of nodeValuations</returns>
        internal static string SerializeValuationsToJson(IDictionary<MeasureSet, decimal> valuationSet)
        {
            const string JsonOuterFormat =
                @"{{
                    ""NodeValuationSet"":
                    [
                        {0}
                    ]
                }}";

            const string JsonInnerFormat = @"{{ ""MeasureSet"":[{0}],""MaxValuation"":{1} }},";
          
            var innerJsonString = new StringBuilder();

            // TODO: verify this gives the appropriate response in the null valuationSet case
            if (valuationSet != null)
            {
                foreach (var nodeValuation in valuationSet)
                {
                    var measureSetString = new StringBuilder();
                    foreach (var measureId in nodeValuation.Key)
                    {
                        var measureString = @"""{0}""".FormatInvariant(measureId);
                        measureSetString.Append(measureString);
                        measureSetString.Append(",");
                    }

                    measureSetString.Remove(measureSetString.Length - 1, 1);

                    innerJsonString.Append(
                        JsonInnerFormat.FormatInvariant(
                            measureSetString,
                            nodeValuation.Value));
                }

                // remove trailing comma
                innerJsonString.Remove(innerJsonString.Length - 1, 1);
            }

            return JsonOuterFormat.FormatInvariant(innerJsonString);
        }

        /// <summary>Creates a CampaignDefinition for DA from a valuation inputs.</summary>
        /// <returns>The CampaignDefinition.</returns>
        internal CampaignDefinition CreateCampaignDefinition()
        {
            // converts slider values directly in valautions using integer division and adds them to the explicit valuation collection. 
            var explicitValuations = this.MeasureSetsInput.Measures.ToDictionary(
                m => new MeasureSet { m.Measure },
                m => ((m.Valuation + 1) / 2) / 100m);

            // if there are overrides, then add those to the explicit valuation collection as well
            if (this.NodeOverrides != null)
            {
                foreach (var theOverride in this.NodeOverrides)
                {
                    var measureSet = new MeasureSet();
                    foreach (var measureId in theOverride.Key)
                    {
                        measureSet.Add(measureId);
                    }

                    explicitValuations.Add(measureSet, theOverride.Value);
                }
            }

            var measureGroupings = this.MeasureSetsInput.Measures.Where(m => !string.IsNullOrWhiteSpace(m.Group)).ToDictionary(m => m.Measure, m => m.Group);
            var pinnedMeasures = this.MeasureSetsInput.Measures.Where(m => m.Pinned).Select(m => m.Measure).ToList();

            return new CampaignDefinition
            {
                ExplicitValuations = explicitValuations,
                MaxPersonaValuation = this.MeasureSetsInput.MaxValuation, // Note that the ideal valuation is used and the max valuation is ignored
                MeasureGroupings = measureGroupings,
                PinnedMeasures = pinnedMeasures,
            };
        }

        /// <summary>Compute a SHA256 hash of the valuation inputs.</summary>
        /// <param name="measureInputsJson">The measure inputs json.</param>
        /// <param name="nodeOverridesJson">The node overrides json.</param>
        /// <returns>The computed hash.</returns>
        private static string ComputeFingerprint(string measureInputsJson, string nodeOverridesJson)
        {
            var sha256Hasher = SHA256Managed.Create();
            var sb = new StringBuilder();
            sb.Append(measureInputsJson);
            sb.Append(string.IsNullOrEmpty(nodeOverridesJson) ? string.Empty : nodeOverridesJson);
            var hash = sha256Hasher.ComputeHash(Encoding.Unicode.GetBytes(sb.ToString()));
            return new PropertyValue(PropertyType.Binary, hash).SerializationValue;
        }
    }
}
