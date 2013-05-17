//-----------------------------------------------------------------------
// <copyright file="IActivitySubmitter.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Threading;

namespace Activities
{
    /// <summary>Interface for submitters of activity requests</summary>
    public interface IActivitySubmitter
    {
        /// <summary>Gets the submitter name</summary>
        string Name { get; }

        /// <summary>Gets the submitter unique id</summary>
        string Id { get; }

        /// <summary>Submits a request and waits for the result</summary>
        /// <param name="request">Activity request</param>
        /// <param name="category">Activity runtime category</param>
        /// <param name="timeout">How long to wait for a result</param>
        /// <returns>The result, if successful; otherwise, false.</returns>
        ActivityResult SubmitAndWait(ActivityRequest request, ActivityRuntimeCategory category, int timeout);

        /// <summary>Submits a request and invokes a callback for the result</summary>
        /// <param name="request">Activity request</param>
        /// <param name="category">Activity runtime category</param>
        /// <param name="callback">Result callback</param>
        /// <returns>True if the activity was successfully submitted; otherwise, false.</returns>
        bool SubmitWithCallback(ActivityRequest request, ActivityRuntimeCategory category, Action<ActivityResult> callback);
    }
}
