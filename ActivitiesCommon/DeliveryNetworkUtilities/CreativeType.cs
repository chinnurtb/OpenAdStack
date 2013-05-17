//-----------------------------------------------------------------------
// <copyright file="CreativeType.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace DeliveryNetworkUtilities
{
    /// <summary>Creative types (values for IEntity.ExternalType)</summary>
    public enum CreativeType
    {
        /// <summary>Unknown creative type</summary>
        Unknown,

        /// <summary>Third Party Ad</summary>
        /// <remarks>Usually an ad tag, width and height</remarks>
        ThirdPartyAd,

        /// <summary>Image Ad</summary>
        /// <remarks>Usually an image (URL or bytes) and a click URL</remarks>
        ImageAd,

        /// <summary>Flash Ad</summary>
        /// <remarks>Flash ad with backup image and flash click variable</remarks>
        FlashAd,

        /// <summary>AppNexus Ad</summary>
        /// <remarks>Reference to an existing AppNexus creative</remarks>
        AppNexus,
    }
}
