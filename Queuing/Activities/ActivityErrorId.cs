//-----------------------------------------------------------------------
// <copyright file="ActivityErrorId.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
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
