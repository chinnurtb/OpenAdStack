//-----------------------------------------------------------------------
// <copyright file="IAuthorizationManager.cs" company="Rare Crowds Inc">
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

namespace Utilities.IdentityFederation
{
    /// <summary>Interface for authorization managers</summary>
    public interface IAuthorizationManager
    {
        /// <summary>
        /// Checks if the authorization context is authorized to perform action
        /// specified in the authorization context on the specified resoure.
        /// </summary>
        /// <param name="action">Action to authorize</param>
        /// <param name="resource">Resource to authorize</param>
        /// <returns>True if authorized; otherwise, false.</returns>
        bool CheckAccess(string action, string resource);
    }
}
