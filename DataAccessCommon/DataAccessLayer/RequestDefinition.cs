// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RequestDefinition.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
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
