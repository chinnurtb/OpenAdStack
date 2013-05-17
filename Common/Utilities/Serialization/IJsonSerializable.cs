//-----------------------------------------------------------------------
// <copyright file="IJsonSerializable.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Utilities.Serialization
{
    /// <summary>Defines an interface for objects that can be serialzied to/from JSON</summary>
    public interface IJsonSerializable
    {
        /// <summary>Serializes the object to its JSON representation</summary>
        /// <returns>The JSON representation of the object</returns>
        string SerializeToJson();

        /// <summary>Initializes the object from its JSON representation</summary>
        /// <param name="json">The JSON representation of the object</param>
        void InitializeFromJson(string json);
    }
}
