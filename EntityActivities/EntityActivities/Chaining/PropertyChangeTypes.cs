//-----------------------------------------------------------------------
// <copyright file="PropertyChangeTypes.cs" company="Rare Crowds Inc">
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

using System;

namespace EntityActivities.Chaining
{
    /// <summary>
    /// Ways in which entity properties can be changed which may result in
    /// chained activity request(s) being submitted.
    /// </summary>
    [Flags]
    public enum PropertyChangeTypes
    {
        /// <summary>No change</summary>
        None = 0x0,
        
        /// <summary>
        /// The original property was null and the updated property is not
        /// </summary>
        Added = 0x1,

        /// <summary>
        /// The original property and the updated property values do not match
        /// </summary>
        Changed = 0x2,

        /// <summary>
        /// The original property was not null and the updated property is
        /// </summary>
        Removed = 0x4,

        /// <summary>
        /// The property was added or the values do not match
        /// </summary>
        AddedOrChanged = Added | Changed,

        /// <summary>
        /// The property was removed or the values do not match
        /// </summary>
        RemovedOrChanged = Removed | Changed,

        /// <summary>
        /// The property was modified in any way
        /// </summary>
        Any = Added | Changed | Removed
    }
}
