//-----------------------------------------------------------------------
// <copyright file="CustomRequestValidator.cs" company="Rare Crowds Inc">
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
using System.Web;
using System.Web.Util;
using Microsoft.IdentityModel.Protocols.WSFederation;

namespace IdentityFederation
{
    /// <summary>
    /// This WebLayerRequestValidator validates the wresult parameter of the
    /// WS-Federation passive protocol by checking for a SignInResponse message
    /// in the form post. The SignInResponse message contents are verified later by
    /// the WSFederationPassiveAuthenticationModule or the WIF signin controls.
    /// </summary>
    public class CustomRequestValidator : RequestValidator
    {
        /// <summary>
        /// Method to determine whether request string is valid
        /// </summary>
        /// <param name="context">Http context</param>
        /// <param name="value">request value</param>
        /// <param name="requestValidationSource">request validation source</param>
        /// <param name="collectionKey">key to collection</param>
        /// <param name="validationFailureIndex">index of error in event of failure</param>
        /// <returns>whether request is valid</returns>
        protected override bool IsValidRequestString(HttpContext context, string value, RequestValidationSource requestValidationSource, string collectionKey, out int validationFailureIndex)
        {
            validationFailureIndex = 0;
            if (requestValidationSource == RequestValidationSource.Form &&
                collectionKey.Equals(WSFederationConstants.Parameters.Result, StringComparison.Ordinal))
            {
                if (WSFederationMessage.CreateFromFormPost(context.Request) as SignInResponseMessage != null)
                {
                    return true;
                }
            }

            return base.IsValidRequestString(context, value, requestValidationSource, collectionKey, out validationFailureIndex);
        }
    }
}