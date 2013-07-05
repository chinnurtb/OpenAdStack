// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReportEntity.cs" company="Rare Crowds Inc">
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

using System;

namespace DataAccessLayer
{
    /// <summary>
    /// An entity wrapper for a report entity.
    /// </summary>
    public class ReportEntity : EntityWrapperBase
    {
        /// <summary>Report Data Property Name for Report Entities.</summary>
        public const string ReportDataName = "ReportData";

        /// <summary>Report Type Property Name for Report Entities.</summary>
        public const string ReportTypeName = "ReportType";

        /// <summary>Category Name for Report Entities.</summary>
        public const string CategoryName = "Report";

        /// <summary>Initializes a new instance of the <see cref="ReportEntity"/> class.</summary>
        /// <param name="entity">The IEntity object from which to construct.</param>
        public ReportEntity(IEntity entity)
        {
            this.Initialize(entity);
        }

        /// <summary>Initializes a new instance of the <see cref="ReportEntity"/> class.</summary>
        /// <param name="externalEntityId">The external entity Id to assign the entity.</param>
        /// <param name="rawEntity">The raw entity from which to construct.</param>
        public ReportEntity(EntityId externalEntityId, IEntity rawEntity)
        {
            this.Initialize(externalEntityId, CategoryName, rawEntity);
        }

        /// <summary>Initializes a new instance of the <see cref="ReportEntity"/> class.</summary>
        public ReportEntity()
        {
        }

        /// <summary>Gets or sets ReportType. Property passed on set can be un-named and name will be set.</summary>
        public string ReportType
        {
            get { return this.GetPropertyByName<string>(ReportTypeName); }
            set { this.SetPropertyByName(ReportTypeName, value); }
        }

        /// <summary>Gets or sets ReportData. Property passed on set can be un-named and name will be set.</summary>
        public string ReportData
        {
            get { return this.GetPropertyByName<string>(ReportDataName); }
            set { this.SetPropertyByName(ReportDataName, value, PropertyFilter.Extended); }
        }

        /// <summary>Factory method to build a report entity.</summary>
        /// <param name="entityId">External Entity Id.</param>
        /// <param name="externalName">External Name.</param>
        /// <param name="reportType">Report Type.</param>
        /// <param name="reportData">Report Data.</param>
        /// <returns>A new blob entity.</returns>
        public static ReportEntity BuildReportEntity(EntityId entityId, string externalName, string reportType, string reportData)
        {
            var wrappedEntity = new Entity
                {
                    ExternalEntityId = entityId,
                    EntityCategory = CategoryName,
                    ExternalName = externalName
                };

            wrappedEntity.SetPropertyByName(ReportTypeName, reportType);
            wrappedEntity.SetPropertyByName(ReportDataName, reportData, PropertyFilter.Extended);

            return new ReportEntity(wrappedEntity);
        }

        /// <summary>Abstract method to validate type of entity.</summary>
        /// <param name="entity">The entity.</param>
        protected override void ValidateEntityType(IEntity entity)
        {
            ThrowIfCategoryMismatch(entity, CategoryName);
            ThrowIfPropertyNotDefined(entity, CategoryName, ReportTypeName);
            ThrowIfPropertyNotDefined(entity, CategoryName, ReportDataName, PropertyFilter.Extended);
        }
    }
}
