//-----------------------------------------------------------------------
// <copyright file="IJsonSerializable.cs" company="Rare Crowds Inc">
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
