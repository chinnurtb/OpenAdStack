//-----------------------------------------------------------------------
// <copyright file="DeliveryNetworkEntityProperties.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace DeliveryNetworkUtilities
{
    /// <summary>
    /// Names of the entity properties in which delivery network related values are kept
    /// </summary>
    public static class DeliveryNetworkEntityProperties
    {
        /// <summary>Delivery network name</summary>
        /// <seealso cref="DynamicAllocation.DeliveryNetworkDesignation"/>
        public const string DeliveryNetwork = "DeliveryNetwork";

        /// <summary>Delivery network exporter version</summary>
        /// <remarks>Also determines measure source factory version</remarks>
        public const string ExporterVersion = "ExporterVersion";

        /// <summary>
        /// Names of delivery network related properties for creatives
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1034", Justification = "Nesting constants containers is okay.")]
        public static class Creative
        {
            /// <summary>Creative 3rd party ad tag</summary>
            public const string AdTag = "Tag";

            /// <summary>Creative width</summary>
            public const string Width = "Width";

            /// <summary>Creative height</summary>
            public const string Height = "Height";

            /// <summary>Creative image bytes</summary>
            public const string ImageBytes = "ImageContent";

            /// <summary>Creative image name</summary>
            public const string ImageName = "ImageName";

            /// <summary>Creative image URL</summary>
            public const string ImageUrl = "ImageUrl";

            /// <summary>Creative click URL</summary>
            public const string ClickUrl = "ClickUrl";

            /// <summary>Creative SWF bytes</summary>
            public const string FlashBytes = "FlashContent";

            /// <summary>Creative SWF name</summary>
            public const string FlashName = "FlashName";

            /// <summary>Creative flash click variable</summary>
            public const string FlashClickVariable = "FlashClickVariable";
        }
    }
}
