//-----------------------------------------------------------------------
// <copyright file="LegacyCampaignConverter.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using AppNexusUtilities;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using DynamicAllocation;
using DynamicAllocationActivities;
using EntityUtilities;
using Utilities.Serialization;

using daName = DynamicAllocationUtilities.DynamicAllocationEntityProperties;

namespace ReportingActivities
{
    /// <summary>Helper methods for converting legacy DA campaigns to current formats.</summary>
    public class LegacyCampaignConverter : IEntityConverter
    {
        /// <summary>Initializes a new instance of the <see cref="LegacyCampaignConverter"/> class. </summary>
        /// <param name="repository">A repository instance.</param>
        public LegacyCampaignConverter(IEntityRepository repository)
        {
            this.Repository = repository;
        }

        /// <summary>Gets the IEntityRepository instance associated with the DA Campaign.</summary>
        internal IEntityRepository Repository { get; private set; }

        /// <summary>Convert an entity to the current schema for that entity.</summary>
        /// <param name="entityToConvert">The entity id to convert.</param>
        /// <param name="companyEntityId">The entity id of the company the entity belongs to.</param>
        public void ConvertEntity(EntityId entityToConvert, EntityId companyEntityId)
        {
            var campaignId = entityToConvert;
            var context = new RequestContext
                {
                    ExternalCompanyId = companyEntityId,
                    EntityFilter = new RepositoryEntityFilter(true, true, true, true)
                };
            var campaignEntity = this.Repository.GetEntity(context, campaignId);

            // Convert measure inputs
            var inputs = this.GetValuationInputsDeprecated(context, campaignEntity, daName.StatusApproved);
            var oldMeasureInputs = inputs.MeasureSetsInput;
            var measureList = new LegacyMeasureListSerialized
            {
                Approved = new LegacyMeasureSetsInputSerialized
                {
                    IdealValuation = oldMeasureInputs.IdealValuation,
                    MaxValuation = oldMeasureInputs.MaxValuation,
                    Measures = oldMeasureInputs.Measures.Select(m => new LegacyMeasuresInputElement
                    {
                        measureId = m.Measure.ToString(CultureInfo.InvariantCulture),
                        group = m.Group,
                        pinned = m.Pinned
                    }).ToList()
                }
            };
            campaignEntity.SetPropertyValueByName(daName.MeasureList, AppsJsonSerializer.SerializeObject(measureList));

            var oldBaseValuations = inputs.BaseValuationSet;
            var baseValuations = new LegacyBaseValuationSetSerialized
            {
                Approved = oldBaseValuations.Select(m =>
                    new LegacyBaseValuationSetSerializedElement
                    {
                        measureId = m.Key.ToString(CultureInfo.InvariantCulture),
                        valuation = m.Value
                    }).ToArray()
            };
            campaignEntity.SetPropertyValueByName("BaseValuationSet", AppsJsonSerializer.SerializeObject(baseValuations));

            // Convert delivery data
            var deliveryDataIndex = new List<string>
                {
                    campaignEntity.TryGetAssociationByName("APNX_RawDeliveryData").TargetEntityId.ToString()
                };

            campaignEntity.SetPropertyValueByName(AppNexusEntityProperties.AppNexusRawDeliveryDataIndex, AppsJsonSerializer.SerializeObject(deliveryDataIndex));

            // Convert node map
            var nodeMapId = campaignEntity.TryGetAssociationByName(daName.AllocationNodeMap).TargetEntityId;
            var oldNodeMapBlob = this.Repository.GetEntity(context, nodeMapId) as BlobEntity;
            var oldNodeMap = oldNodeMapBlob.DeserializeBlob<Dictionary<string, long[]>>();
            var newNodeMapBlob = BlobEntity.BuildBlobEntity(
                new EntityId(), oldNodeMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(m => m).ToArray())) as IEntity;
            if (!this.Repository.TrySaveEntity(context, newNodeMapBlob))
            {
                throw new ArgumentException("Could not convert legacy campaign - save node map.");
            }

            campaignEntity.TryAssociateEntities(
                daName.AllocationNodeMap, string.Empty, new HashSet<IEntity> { newNodeMapBlob }, AssociationType.Relationship, true);

            // Set delivery network
            campaignEntity.SetPropertyValueByName(
                DeliveryNetworkEntityProperties.DeliveryNetwork, DeliveryNetworkDesignation.AppNexus.ToString());

            // Set margin and per mille fees overrides to what they were at the time the legacy campaigns were run
            var config = campaignEntity.GetConfigSettings();
            config["DynamicAllocation.Margin"] = "{0}".FormatInvariant(1m / 0.85m);
            config["DynamicAllocation.PerMilleFees"] = "{0}".FormatInvariant(0.06m);
            campaignEntity.SetConfigSettings(config);

            if (!this.Repository.TrySaveEntity(context, campaignEntity))
            {
                throw new ArgumentException("Could not convert legacy campaign - save campaign.");
            }
        }

        /// <summary>Get ValuationInputs From Campaign Entity associations (deprecated).</summary>
        /// <param name="requestContext">the requestContext</param>
        /// <param name="campaignEntity">the campaign</param>
        /// <param name="requestedValuationStatus">the status of the valuations requested (draft or approved)</param>
        /// <returns>ValuationInputs or null if not successful.</returns>
        internal LegacyValuationInputs GetValuationInputsDeprecated(
            RequestContext requestContext, IEntity campaignEntity, string requestedValuationStatus)
        {
            // allow node valuation set to be null
            IDictionary<MeasureSet, decimal> nodeOverrides = null;
            var nodeOverridesEntityOrNull = campaignEntity.TryGetAssociationByName("NodeValuationSet" + requestedValuationStatus);
            if (nodeOverridesEntityOrNull != null)
            {
                var nodeOverridesEntityId = nodeOverridesEntityOrNull.TargetEntityId;
                var nodeOverridesEntity = this.Repository.GetEntity(requestContext, nodeOverridesEntityId);
                var nodeOverridesBlob = nodeOverridesEntity as BlobEntity;
                var nodeOverridesJson = nodeOverridesBlob.DeserializeBlob<string>();
                nodeOverrides = LegacyValuationInputs.DeserializeNodeOverridesJsonDeprecated(nodeOverridesJson);
            }

            var measureSetEntityId = campaignEntity.TryGetAssociationByName("MeasureSet" + requestedValuationStatus).TargetEntityId;
            var measureSetJson =
                (this.Repository.GetEntity(requestContext, measureSetEntityId) as BlobEntity).DeserializeBlob<string>();
            var measures = LegacyValuationInputs.DeserializeMeasuresJsonDeprecated(measureSetJson);

            var baseValuationSetEntityId = campaignEntity.TryGetAssociationByName("BaseValuationSet" + requestedValuationStatus).TargetEntityId;
            var baseValuationSetJson =
                (this.Repository.GetEntity(requestContext, baseValuationSetEntityId) as BlobEntity).DeserializeBlob<string>();
            var baseValuationSet = LegacyValuationInputs.DeserializeBaseValuationSetJsonDeprecated(baseValuationSetJson);

            return new LegacyValuationInputs(measures, baseValuationSet, nodeOverrides);
        }

        /// <summary>
        /// Class to encapsulate valuation inputs serialization and convert to a CampaignDefinition for consumption by.
        /// dynamic allocation.
        /// </summary>
        internal class LegacyValuationInputs
        {
            /// <summary>Initializes a new instance of the <see cref="LegacyValuationInputs"/> class.</summary>
            /// <param name="measureInputs">The measure inputs.</param>
            /// <param name="baseValuationSet">The base valuations.</param>
            /// <param name="nodeOverrides">The node overrides.</param>
            internal LegacyValuationInputs(
                MeasureSetsInput measureInputs,
                IDictionary<long, int> baseValuationSet,
                IDictionary<MeasureSet, decimal> nodeOverrides)
            {
                this.MeasureSetsInput = measureInputs;
                this.BaseValuationSet = baseValuationSet;
                this.NodeOverrides = nodeOverrides;
            }

            /// <summary>Gets the Node overrides</summary>
            internal IDictionary<MeasureSet, decimal> NodeOverrides { get; private set; }

            /// <summary>
            /// Gets the BaseValuationSets (sliders): 
            /// sliders go from 1 -> 100 and represent relative importances. Sliders are currently directly translated into valuations from .01->.50
            /// </summary>
            internal IDictionary<long, int> BaseValuationSet { get; private set; }

            /// <summary>
            /// Gets an enumerable of the MeasuresInputs
            /// a measuresinput contains the measure, the OR grouping for the measure, and a bool for if it's pinned
            /// </summary>
            internal MeasureSetsInput MeasureSetsInput { get; private set; }

            /// <summary>Deserialize Measures Json</summary>
            /// <param name="measuresJson">measures Json</param>
            /// <returns>rehydrated MeasureSetsInput</returns>
            internal static MeasureSetsInput DeserializeMeasuresJsonDeprecated(string measuresJson)
            {
                var jsonMeasureSetsList =
                    AppsJsonSerializer.DeserializeObject<Dictionary<string, object>>(measuresJson);
                var jsonMeasureSetsArray = jsonMeasureSetsList["MeasureSets"] as Dictionary<string, object>;
                var idealValuation = Convert.ToDecimal(
                    jsonMeasureSetsArray["IdealValuation"], CultureInfo.InvariantCulture);
                var maxValuation = Convert.ToDecimal(jsonMeasureSetsArray["MaxValuation"], CultureInfo.InvariantCulture);
                var measuresList = (jsonMeasureSetsArray["Measures"] as ArrayList).ToArray();

                var measures = new List<MeasuresInput>();
                foreach (var measuresObject in measuresList)
                {
                    var measuresDictionary = measuresObject as Dictionary<string, object>;
                    measures.Add(
                        new MeasuresInput
                            {
                                Measure = Convert.ToInt64(measuresDictionary["measureId"], CultureInfo.InvariantCulture),
                                Group = Convert.ToString(measuresDictionary["group"], CultureInfo.InvariantCulture),
                                Pinned = Convert.ToBoolean(measuresDictionary["pinned"], CultureInfo.InvariantCulture)
                            });
                }

                return new MeasureSetsInput
                    {
                        Measures = measures, MaxValuation = maxValuation, IdealValuation = idealValuation,
                    };
            }

            /// <summary>
            /// Deserialize BaseValuationSet Json
            /// </summary>
            /// <param name="baseValuationSetJson">base Valuation Set Json </param>
            /// <returns>Dictionary from Measure to int</returns>
            internal static IDictionary<long, int> DeserializeBaseValuationSetJsonDeprecated(
                string baseValuationSetJson)
            {
                var jsonBaseValuationSetList =
                    AppsJsonSerializer.DeserializeObject<Dictionary<string, object>>(baseValuationSetJson);
                var baseValuationSetList = (jsonBaseValuationSetList["valuationSet"] as ArrayList).ToArray();

                var baseValuationSet = new Dictionary<long, int>();
                foreach (IDictionary<string, object> baseValuationDictionary in baseValuationSetList)
                {
                    var measureId = Convert.ToInt64(baseValuationDictionary["measureId"], CultureInfo.InvariantCulture);
                    var value = Convert.ToInt32(baseValuationDictionary["valuation"], CultureInfo.InvariantCulture);

                    baseValuationSet.Add(measureId, value);
                }

                return baseValuationSet;
            }

            /// <summary>
            /// Deserialize Node Valuation Set Json
            /// </summary>
            /// <param name="nodeValuationSetJson">node Valuation Set Json</param>
            /// <returns>Dictionary from MeasureSet to decimal</returns>
            internal static IDictionary<MeasureSet, decimal> DeserializeNodeOverridesJsonDeprecated(
                string nodeValuationSetJson)
            {
                var jsonNodeValuationSetList =
                    AppsJsonSerializer.DeserializeObject<Dictionary<string, object>>(nodeValuationSetJson);
                var nodeValuationSetList = (jsonNodeValuationSetList["NodeValuationSet"] as ArrayList).ToArray();

                var nodeValuationSet = new Dictionary<MeasureSet, decimal>();
                foreach (IDictionary<string, object> nodeValuationDictionary in nodeValuationSetList)
                {
                    var nodeValuationSetArrayList = (nodeValuationDictionary["MeasureSet"] as ArrayList).ToArray();
                    var measureSet =
                        nodeValuationSetArrayList.Select(
                            measureId => Convert.ToInt64(measureId, CultureInfo.InvariantCulture));
                    var value = Convert.ToDecimal(nodeValuationDictionary["MaxValuation"], CultureInfo.InvariantCulture);

                    nodeValuationSet.Add(new MeasureSet(measureSet), value);
                }

                return nodeValuationSet;
            }
        }

        /// <summary>Class representing the serialized form of the MeasureList valuation inputs.</summary>
        internal class LegacyMeasureListSerialized
        {
            /// <summary>
            /// Gets or sets Draft measure list.
            /// </summary>
            public LegacyMeasureSetsInputSerialized Draft { get; set; }

            /// <summary>
            /// Gets or sets Approved measure list.
            /// </summary>
            public LegacyMeasureSetsInputSerialized Approved { get; set; }
        }

        /// <summary>Class representing the serialized form of the MeasureSetsInput valuation inputs.</summary>
        internal class LegacyMeasureSetsInputSerialized
        {
            /// <summary>
            /// Gets or sets the IdealValuation
            /// </summary>
            public decimal IdealValuation { get; set; }

            /// <summary>
            /// Gets or sets the MaxValuation
            /// </summary>
            public decimal MaxValuation { get; set; }

            /// <summary>
            /// Gets or sets the Measures
            /// </summary>
            [SuppressMessage("Microsoft.Usage", "CA2227", Justification = "Serialization object")]
            public IList<LegacyMeasuresInputElement> Measures { get; set; }
        }

        /// <summary>
        /// Class representing the serialized form of a base valuation set.
        /// </summary>
        internal class LegacyBaseValuationSetSerialized
        {
            /// <summary>
            /// Gets or sets Draft base valuations.
            /// </summary>
            [SuppressMessage("Microsoft.Usage", "CA2227", Justification = "Serialization object")]
            public IList<LegacyBaseValuationSetSerializedElement> Draft { get; set; }

            /// <summary>
            /// Gets or sets Approved base valuations.
            /// </summary>
            [SuppressMessage("Microsoft.Usage", "CA2227", Justification = "Serialization object")]
            public IList<LegacyBaseValuationSetSerializedElement> Approved { get; set; }
        }

        /// <summary>Collection element for base valuation set.</summary>
        internal class LegacyBaseValuationSetSerializedElement
        {
            /// <summary>Gets or sets measureId.</summary>
            [SuppressMessage("Microsoft.Naming", "CA1709", Justification = "Serialization object")]
            [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Serialization object")]
            public string measureId { get; set; }

            /// <summary>Gets or sets valuation.</summary>
            [SuppressMessage("Microsoft.Naming", "CA1709", Justification = "Serialization object")]
            [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Serialization object")]
            public int valuation { get; set; }
        }

        /// <summary>Class representing the serialized form of the MeasureInput valuation inputs.</summary>
        internal class LegacyMeasuresInputElement
        {
            /// <summary>
            /// Gets or sets measureId: the measure
            /// </summary>
            [SuppressMessage("Microsoft.Naming", "CA1709", Justification = "Serialization object")]
            [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Serialization object")]
            public string measureId { get; set; }

            /// <summary>
            /// Gets or sets Group: the OR Group the measure belongs to (may be null if it belongs to its own group)
            /// </summary>
            [SuppressMessage("Microsoft.Naming", "CA1709", Justification = "Serialization object")]
            [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Serialization object")]
            public string @group { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the measure is pinned
            /// </summary>
            [SuppressMessage("Microsoft.Naming", "CA1709", Justification = "Serialization object")]
            [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Serialization object")]
            public bool pinned { get; set; }
        }
        
        /// <summary>
        /// Class representing the serialized form of a node valuation set.
        /// </summary>
        internal class LegacyNodeValuationSetSerialized
        {
            /// <summary>
            /// Gets or sets Draft node valuation set.
            /// </summary>
            [SuppressMessage("Microsoft.Usage", "CA2227", Justification = "Serialization object")]
            public IList<NodeValuationSerializationElement> Draft { get; set; }

            /// <summary>
            /// Gets or sets Approved node valuation set.
            /// </summary>
            [SuppressMessage("Microsoft.Usage", "CA2227", Justification = "Serialization object")]
            public IList<NodeValuationSerializationElement> Approved { get; set; }
        }
    }
}
