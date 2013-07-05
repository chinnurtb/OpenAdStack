// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultActivityHandler.cs" company="Rare Crowds Inc">
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

namespace Activities
{
    /// <summary>
    /// Default (no-op) activity handler
    /// </summary>
    public class DefaultActivityHandler : IActivityHandler
    {
        /// <summary>Execute the activity handler.</summary>
        /// <returns>The activity result.</returns>
        public IDictionary<string, string> Execute()
        {
            return new Dictionary<string, string>();
        }
    }
}