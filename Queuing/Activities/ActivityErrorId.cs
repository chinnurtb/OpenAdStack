//-----------------------------------------------------------------------
// <copyright file="ActivityErrorId.cs" company="Rare Crowds Inc">
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

namespace Activities
{
    /// <summary>
    /// Enumeration of possible values for ActivityError.ErrorId
    /// </summary>
    public enum ActivityErrorId
    {
        /// <summary>No Error</summary>
        None = 0,

        /// <summary>Generic error</summary>
        GenericError,

        /// <summary>Missing one or more required input values</summary>
        MissingRequiredInput,

        /// <summary>One or more result values were not one of those allowed</summary>
        UnknownResultValue,

        /// <summary>An error occured deserializing the JSON</summary>
        InvalidJson,

        /// <summary>An error occured within the DataAccessLayer</summary>
        /// <remarks>TODO: Remove</remarks>
        DataAccess,

        /// <summary>No entity exists for the specified Id</summary>
        /// <remarks>TODO: Remove</remarks>
        InvalidEntityId,

        /// <summary>Faliure Sending email</summary>
        /// <remarks>TODO: Remove</remarks>
        FailureSendingEmail,

        /// <summary>
        /// User did not have permisssion to perform the action in the activity
        /// </summary>
        UserAccessDenied
    }
}
