// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ODataElement.cs" company="Rare Crowds Inc">
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
