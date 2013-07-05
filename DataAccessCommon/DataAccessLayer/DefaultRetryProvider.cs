// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultRetryProvider.cs" company="Rare Crowds Inc">
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
using System.Threading;
using Diagnostics;

namespace DataAccessLayer
{
    /// <summary>Default retry provider.</summary>
    internal class DefaultRetryProvider : IRetryProvider
    {
        /// <summary>Initializes a new instance of the <see cref="DefaultRetryProvider"/> class.</summary>
        /// <param name="maxTries">Maximum number retries.</param>
        /// <param name="waitTime">Wait time between retryable failures.</param>
        public DefaultRetryProvider(int maxTries, int waitTime)
        {
            this.MaxRetries = maxTries - 1;
            this.WaitTime = waitTime;
        }

        /// <summary>Gets the maximum number of tries.</summary>
        public int MaxRetries { get; private set; }

        /// <summary>Gets the wait time</summary>
        public int WaitTime { get; private set; }

        /// <summary>Make a copy of the retry providers.</summary>
        /// <returns>A new IRetryProvider.</returns>
        public IRetryProvider Clone()
        {
            return new DefaultRetryProvider(this.MaxRetries + 1, this.WaitTime);
        }

        /// <summary>Handle throwable retry behavior.</summary>
        /// <param name="exception">The exception to be thrown.</param>
        /// <param name="remainingRetryCount">The attempts remaining.</param>
        /// <param name="canRetry">True to attempt retry.</param>
        /// <param name="canWait">True to wait before retry.</param>
        public void RetryOrThrow(Exception exception, ref int remainingRetryCount, bool canRetry, bool canWait)
        {
            if (!canRetry || remainingRetryCount <= 0)
            {
                var msg = "Failed save selected of entity after retry limit: {0}".FormatInvariant(exception.ToString());
                LogManager.Log(LogLevels.Error, true, msg);
                throw exception;
            }

            if (canWait)
            {
                Thread.Sleep(this.WaitTime);
            }

            remainingRetryCount--;
        }
    }
}