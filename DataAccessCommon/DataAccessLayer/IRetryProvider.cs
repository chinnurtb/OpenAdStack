// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IRetryProvider.cs" company="Rare Crowds Inc">
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

using System;

namespace DataAccessLayer
{
    /// <summary>
    /// Interface definition for retry behavior.
    /// </summary>
    public interface IRetryProvider
    {
        /// <summary>Gets the maximum number of tries.</summary>
        int MaxRetries { get; }

        /// <summary>Gets the wait time</summary>
        int WaitTime { get; }

        /// <summary>Make a copy of the retry providers.</summary>
        /// <returns>A new IRetryProvider.</returns>
        IRetryProvider Clone();
        
        /// <summary>Handle throwable retry behavior.</summary>
        /// <param name="exception">The exception to be thrown.</param>
        /// <param name="remainingTryCount">The attempts remaining.</param>
        /// <param name="canRetry">True to attempt retry.</param>
        /// <param name="canWait">True to wait before retry.</param>
        void RetryOrThrow(Exception exception, ref int remainingTryCount, bool canRetry, bool canWait);
    }
}