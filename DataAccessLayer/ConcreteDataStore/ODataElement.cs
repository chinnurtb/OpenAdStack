// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ODataElement.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ConcreteDataStore
{
    /// <summary>Definition of an odata element for use in entity serialization.</summary>
    internal class ODataElement
    {
        /// <summary>Gets or sets a value indicating whether the OData element has a value.</summary>
        public bool HasValue { get; set; }

        /// <summary>Gets or sets a value indicating the OData type of the element.</summary>
        public string ODataType { get; set; }

        /// <summary>Gets or sets a value indicating the OData name of the element.</summary>
        public string ODataName { get; set; }

        /// <summary>Gets or sets a value indicating the OData value of the element.</summary>
        public string ODataValue { get; set; }
    }
}
