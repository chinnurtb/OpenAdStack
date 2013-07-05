//-----------------------------------------------------------------------
// <copyright file="ThreadingExtensions.cs" company="Rare Crowds Inc">
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

using System.Threading.Tasks;

namespace Utilities.Threading
{
    /// <summary>Extension method related to threading</summary>
    public static class ThreadingExtensions
    {
        /// <summary>Wait for the task to complete and return its result</summary>
        /// <typeparam name="TResult">Type of the result</typeparam>
        /// <param name="task">The task</param>
        /// <returns>The result</returns>
        public static TResult WaitForResult<TResult>(this Task<TResult> task)
        {
            task.Wait();
            return task.Result;
        }

        /// <summary>Wait for the task to complete and return its result</summary>
        /// <typeparam name="TResult">Type of the result</typeparam>
        /// <param name="task">The task</param>
        /// <param name="millisecondsTimeout">Wait timeout in milliseconds</param>
        /// <returns>The result</returns>
        public static TResult WaitForResult<TResult>(this Task<TResult> task, int millisecondsTimeout)
        {
            if (!task.Wait(millisecondsTimeout))
            {
                // TODO: Throw exception
                return default(TResult);
            }

            return task.Result;
        }
    }
}
