//-----------------------------------------------------------------------
// <copyright file="ThreadingExtensions.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
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
