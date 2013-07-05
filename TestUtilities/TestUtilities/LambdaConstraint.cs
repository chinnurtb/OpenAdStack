// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LambdaConstraint.cs" company="Rare Crowds Inc">
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
using Rhino.Mocks.Constraints;

namespace TestUtilities
{
    /// <summary>RhinoMocks AbstractConstraint taking a user supplied lambda expression evaluator.</summary>
    /// <typeparam name="T">The type of the object to be evaluated.</typeparam>
    public class LambdaConstraint<T> : AbstractConstraint
    {
        /// <summary>lambda expression evaluating an object of type T and returning boolean result.</summary>
        private Func<T, bool> lambdaEvaluator;

        /// <summary>Initializes a new instance of the <see cref="LambdaConstraint{T}"/> class.</summary>
        /// <param name="evaluator">The constraint evaluator</param>
        public LambdaConstraint(Func<T, bool> evaluator)
        {
            this.lambdaEvaluator = evaluator;
        }

        /// <summary>
        /// Gets the message for this constraint
        /// </summary>
        /// <value/>
        public override string Message
        {
            get { return "Generic LambdaConstraint"; }
        }

        /// <summary>Determines if the object pass the constraints</summary>
        /// <param name="obj">The object of type T to evaluate.</param>
        /// <returns>result of constraint evaluation</returns>
        public override bool Eval(object obj)
        {
            return this.lambdaEvaluator((T)obj);
        }
    }
}
