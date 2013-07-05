//-----------------------------------------------------------------------
// <copyright file="DeliveryNetworkEntityExtensions.cs" company="Emerging Media Group">
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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using DataAccessLayer;
using Diagnostics;
using DynamicAllocation;
using EntityUtilities;

namespace DeliveryNetworkUtilities
{
    /// <summary>
    /// Extensions for getting/setting delivery network properties from Entities
    /// </summary>
    public static class DeliveryNetworkEntityExtensions
    {
        /// <summary>Gets the third party ad tag for a creative</summary>
        /// <param name="this">The creative</param>
        /// <returns>The tag</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CreativeEntity")]
        public static string GetThirdPartyAdTag(this CreativeEntity @this)
        {
            var value = @this.TryGetPropertyValueByName(DeliveryNetworkEntityProperties.Creative.AdTag);
            return value != null ? (string)value : null;
        }

        /// <summary>Gets the width of a creative</summary>
        /// <param name="this">The creative</param>
        /// <returns>The width</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CreativeEntity")]
        public static int? GetWidth(this CreativeEntity @this)
        {
            return @this.TryGetNumericPropertyValue(DeliveryNetworkEntityProperties.Creative.Width);
        }

        /// <summary>Gets the height of a creative</summary>
        /// <param name="this">The creative</param>
        /// <returns>The height</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CreativeEntity")]
        public static int? GetHeight(this CreativeEntity @this)
        {
            return @this.TryGetNumericPropertyValue(DeliveryNetworkEntityProperties.Creative.Height);
        }

        /// <summary>Gets the image URL of a creative</summary>
        /// <param name="this">The creative</param>
        /// <returns>The image URL</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CreativeEntity")]
        [SuppressMessage("Microsoft.Design", "CA1055", Justification = "Image URL is always used as a string")]
        public static string GetImageUrl(this CreativeEntity @this)
        {
            var value = @this.TryGetPropertyValueByName(DeliveryNetworkEntityProperties.Creative.ImageUrl);
            return value != null ? (string)value : null;
        }

        /// <summary>Gets the image file name of a creative</summary>
        /// <param name="this">The creative</param>
        /// <returns>The image file name</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CreativeEntity")]
        public static string GetImageName(this CreativeEntity @this)
        {
            var value = @this.TryGetPropertyValueByName(DeliveryNetworkEntityProperties.Creative.ImageName);
            return value != null ? (string)value : null;
        }

        /// <summary>Gets the image bytes of a creative</summary>
        /// <param name="this">The creative</param>
        /// <returns>The image bytes</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CreativeEntity")]
        public static byte[] GetImageBytes(this CreativeEntity @this)
        {
            var value = @this.TryGetPropertyValueByName(DeliveryNetworkEntityProperties.Creative.ImageBytes);
            return value != null ? Convert.FromBase64String((string)value) : null;
        }

        /// <summary>Gets the flash file name of a creative</summary>
        /// <param name="this">The creative</param>
        /// <returns>The flash file name</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CreativeEntity")]
        public static string GetFlashName(this CreativeEntity @this)
        {
            var value = @this.TryGetPropertyValueByName(DeliveryNetworkEntityProperties.Creative.FlashName);
            return value != null ? (string)value : null;
        }

        /// <summary>Gets the flash bytes of a creative</summary>
        /// <param name="this">The creative</param>
        /// <returns>The flash bytes</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CreativeEntity")]
        public static byte[] GetFlashBytes(this CreativeEntity @this)
        {
            var value = @this.TryGetPropertyValueByName(DeliveryNetworkEntityProperties.Creative.FlashBytes);
            return value != null ? Convert.FromBase64String((string)value) : null;
        }

        /// <summary>Gets the flash click variable for a creative</summary>
        /// <param name="this">The creative</param>
        /// <returns>The flash click variable</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CreativeEntity")]
        public static string GetFlashClickVariable(this CreativeEntity @this)
        {
            var value = @this.TryGetPropertyValueByName(DeliveryNetworkEntityProperties.Creative.FlashClickVariable);
            return value != null ? (string)value : null;
        }

        /// <summary>Gets the width of a creative</summary>
        /// <param name="this">The creative</param>
        /// <returns>The click URL</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CreativeEntity")]
        [SuppressMessage("Microsoft.Design", "CA1055", Justification = "Click URL is always used as a string")]
        public static string GetClickUrl(this CreativeEntity @this)
        {
            var value = @this.TryGetPropertyValueByName(DeliveryNetworkEntityProperties.Creative.ClickUrl);
            return value != null ? (string)value : null;
        }

        /// <summary>Gets the Delivery Network for an entity</summary>
        /// <param name="entity">The entity</param>
        /// <returns>Delivery Network</returns>
        public static DeliveryNetworkDesignation GetDeliveryNetwork(this IEntity entity)
        {
            var deliveryNetworkName = entity.TryGetPropertyValueByName(DeliveryNetworkEntityProperties.DeliveryNetwork);
            if (deliveryNetworkName == null)
            {
                LogManager.Log(
                    LogLevels.Trace,
                    "No Delivery Network set on {0} '{1}' ({2})",
                    entity.EntityCategory,
                    entity.ExternalName,
                    entity.ExternalEntityId);
                return default(DeliveryNetworkDesignation);
            }

            DeliveryNetworkDesignation deliveryNetwork;
            if (!Enum.TryParse<DeliveryNetworkDesignation>(deliveryNetworkName, true, out deliveryNetwork))
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "Invalid Delivery Network set on {0} '{1}' ({2}): {3}",
                    entity.EntityCategory,
                    entity.ExternalName,
                    entity.ExternalEntityId,
                    deliveryNetworkName);
            }

            return deliveryNetwork;
        }

        /// <summary>Sets the Delivery Network for an entity</summary>
        /// <param name="entity">The entity</param>
        /// <param name="deliveryNetwork">The delivery network</param>
        public static void SetDeliveryNetwork(this IEntity entity, DeliveryNetworkDesignation deliveryNetwork)
        {
            entity.SetPropertyValueByName(DeliveryNetworkEntityProperties.DeliveryNetwork, deliveryNetwork.ToString());
        }

        /// <summary>Gets the version of the measure sources used with an entity</summary>
        /// <param name="entity">The entity</param>
        /// <returns>If present, the exporter version to use; otherwise, 0</returns>
        public static int GetExporterVersion(this IEntity entity)
        {
            return (int)@entity.TryGetSystemProperty<double>(DeliveryNetworkEntityProperties.ExporterVersion);
        }

        /// <summary>Sets the version of the exporter to use with an entity</summary>
        /// <param name="entity">The entity</param>
        /// <param name="exporterVersion">The exporter version</param>
        public static void SetExporterVersion(this IEntity entity, int exporterVersion)
        {
            entity.SetSystemProperty(DeliveryNetworkEntityProperties.ExporterVersion, (double)exporterVersion);
        }

        /// <summary>Sets the type of creative as the CreativeEntity.ExternalType</summary>
        /// <param name="this">The CreativeEntity</param>
        /// <param name="creativeType">The creative type</param>
        /// <exception cref="ArgumentException">Attempted to set CreativeType.Unknown</exception>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CreativeEntity")]
        public static void SetCreativeType(this CreativeEntity @this, CreativeType creativeType)
        {
            if (creativeType == CreativeType.Unknown)
            {
                throw new ArgumentException("Cannot set creative type to {0}".FormatInvariant(creativeType), "creativeType");
            }

            @this.ExternalType = creativeType.ToString();
        }

        /// <summary>Gets the type of creative from the CreativeEntity.ExternalType</summary>
        /// <param name="this">The CreativeEntity</param>
        /// <returns>The creative type</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CreativeEntity")]
        public static CreativeType GetCreativeType(this CreativeEntity @this)
        {
            CreativeType value;
            return Enum.TryParse<CreativeType>(@this.ExternalType, true, out value) ? value :
                @this.ExternalType.ToString() == "AdThirdParty" ? CreativeType.ThirdPartyAd : // Back-compat w/ old creative type
                CreativeType.Unknown;
        }
    }
}
