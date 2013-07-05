//-----------------------------------------------------------------------
// <copyright file="ActivityError.cs" company="Rare Crowds Inc">
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
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;

namespace Activities
{
    /// <summary>
    /// Describes an error that occured during processing
    /// </summary>
    [DataContract]
    public class ActivityError
    {
        /// <summary>
        /// Initializes a new instance of the ActivityError class.
        /// </summary>
        public ActivityError()
        {
            this.ErrorId = -1;
            this.Message = string.Empty;
            this.StackTrace = new StackTrace(1, true).ToString();
        }

        /// <summary>
        /// Gets the stack trace
        /// </summary>
        public string StackTrace { get; private set; }

        /// <summary>
        /// Gets or sets the identifier of the error that occured
        /// </summary>
        [DataMember]
        public int ErrorId { get; set; }

        /// <summary>
        /// Gets or sets a message describing details about the error
        /// </summary>
        /// <remarks>Can be suppressed for external use</remarks>
        [DataMember]
        public string Message { get; set; }

        /// <summary>
        /// Serializes to json
        /// </summary>
        /// <returns>the json</returns>
        public string SerializeToJson()
        {
            return new JavaScriptSerializer().Serialize(this);
        }
    }
}
