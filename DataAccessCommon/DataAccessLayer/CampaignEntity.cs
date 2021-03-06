// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CampaignEntity.cs" company="Rare Crowds Inc">
// Copyright 2012-2013 Rare Crowds, Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DataAccessLayer
{
    /// <summary>
    /// Wrapped Entity with validating members for Campaign.
    /// No Default construction - use Enity or JSON string
    /// to initialize.
    /// </summary>
    public class CampaignEntity : EntityWrapperBase
    {
        /// <summary>Budget property name.</summary>
        public const string BudgetPropertyName = "Budget";

        /// <summary>StartDate property name.</summary>
        public const string StartDatePropertyName = "StartDate";

        /// <summary>EndDate property name.</summary>
        public const string EndDatePropertyName = "EndDate";

        /// <summary>PersonaName property name.</summary>
        public const string PersonaNamePropertyName = "PersonaName";

        /// <summary>Category Name for Campaign Entities.</summary>
        public const string CategoryName = "Campaign";

        /// <summary>Initializes a new instance of the <see cref="CampaignEntity"/> class.</summary>
        /// <param name="externalEntityId">The external entity id to assign the entity.</param>
        /// <param name="rawEntity">The raw entity from which to construct.</param>
        public CampaignEntity(EntityId externalEntityId, IEntity rawEntity)
        {
            this.Initialize(externalEntityId, CategoryName, rawEntity);
        }

        /// <summary>Initializes a new instance of the <see cref="CampaignEntity"/> class.</summary>
        /// <param name="entity">The IEntity object from which to construct.</param>
        public CampaignEntity(IEntity entity)
        {
            this.Initialize(entity);
        }

        /// <summary>Initializes a new instance of the <see cref="CampaignEntity"/> class.</summary>
        public CampaignEntity()
        {
        }

        /// <summary>Gets or sets Budget. Property passed on set can be un-named and name will be set.</summary>
        public EntityProperty Budget
        {
            get { return this.TryGetEntityPropertyByName(BudgetPropertyName, string.Empty); }
            set { this.SetEntityProperty(new EntityProperty { Name = BudgetPropertyName, Value = value.Value }); }
        }

        /// <summary>Gets or sets StartDate. Property passed on set can be un-named and name will be set.</summary>
        public EntityProperty StartDate
        {
            get { return this.TryGetEntityPropertyByName(StartDatePropertyName, string.Empty); }
            set { this.SetEntityProperty(new EntityProperty { Name = StartDatePropertyName, Value = value.Value }); }
        }

        /// <summary>Gets or sets EndDate. Property passed on set can be un-named and name will be set.</summary>
        public EntityProperty EndDate
        {
            get { return this.TryGetEntityPropertyByName(EndDatePropertyName, string.Empty); }
            set { this.SetEntityProperty(new EntityProperty { Name = EndDatePropertyName, Value = value.Value }); }
        }

        /// <summary>Gets or sets PersonaName. Property passed on set can be un-named and name will be set.</summary>
        public EntityProperty PersonaName
        {
            get { return this.TryGetEntityPropertyByName(PersonaNamePropertyName, string.Empty); }
            set { this.SetEntityProperty(new EntityProperty { Name = PersonaNamePropertyName, Value = value.Value }); }
        }

        /// <summary>Abstract method to validate type of entity.</summary>
        /// <param name="entity">The entity.</param>
        protected override void ValidateEntityType(IEntity entity)
        {
            // TODO: Determine appropriate type validation for campaign
            ThrowIfCategoryMismatch(entity, CategoryName);
        }
    }
}
