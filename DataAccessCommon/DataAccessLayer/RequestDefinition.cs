// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RequestDefinition.cs" company="Rare Crowds Inc">
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

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DataAccessLayer
{
    /// <summary>
    /// Object encapsulating the abstract definition (xml) of partner defined request type
    /// </summary>
    public class RequestDefinition
    {
        /// <summary>Initializes a new instance of the <see cref="RequestDefinition"/> class.</summary>
        /// <param name="definitionXml">The definition xml.</param>
        public RequestDefinition(string definitionXml)
        {
            this.DefinitionXml = XElement.Parse(definitionXml);

            var instructions = this.DefinitionXml.Elements().SingleOrDefault(e => e.Name == "InstructionSequence");
            this.InstructionSequence = instructions != null ? instructions.Elements() : null;

            var entityTypes = this.DefinitionXml.Elements().SingleOrDefault(e => e.Name == "EntityTypes");
            this.EntityTypeInfo = entityTypes != null ? entityTypes.Elements() : null;
        }

        /// <summary>Gets DefinitionXml.</summary>
        public XElement DefinitionXml { get; private set; }

        /// <summary>Gets EntityTypes.</summary>
        public IEnumerable<XElement> EntityTypeInfo { get; private set; }

        /// <summary>Gets InstructionSequence.</summary>
        public IEnumerable<XElement> InstructionSequence { get; private set; }
    }
}
