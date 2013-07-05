//-----------------------------------------------------------------------
// <copyright file="IActivitySubmitter.cs" company="Rare Crowds Inc">
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
