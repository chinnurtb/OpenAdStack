// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActivityException.cs" company="Rare Crowds Inc">
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
using System.Runtime.Serialization;
using Utilities;

namespace Activities
{
    /// <summary>Custom exception for Activity errors.</summary>
    [Serializable]
    public class ActivityException : AppsGenericException
    {
        /// <summary>The name for the serialization info dictionary.</summary>
        internal const string ActivityErrorIdSerializationName = "activity_error_id";

        /// <summary>Initializes a new instance of the <see cref="ActivityException"/> class.</summary>
        public ActivityException()
            : this(ActivityErrorId.None)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ActivityException"/> class.</summary>
        /// <param name="errorId">ActivityErrorId for the exception.</param>
        public ActivityException(ActivityErrorId errorId)
        {
            this.ActivityErrorId = errorId;
        }

        /// <summary>Initializes a new instance of the <see cref="ActivityException"/> class.</summary>
        /// <param name="message">Message for the exception.</param>
        public ActivityException(string message)
            : this(ActivityErrorId.None, message)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ActivityException"/> class.</summary>
        /// <param name="errorId">ActivityErrorId for the exception.</param>
        /// <param name="message">Message for the exception.</param>
        public ActivityException(ActivityErrorId errorId, string message)
            : base(message)
        {
            this.ActivityErrorId = errorId;
        }

        /// <summary>Initializes a new instance of the <see cref="ActivityException"/> class.</summary>
        /// <param name="message">Message for the exception.</param>
        /// <param name="inner">The inner exception.</param>
        public ActivityException(string message, Exception inner)
            : this(ActivityErrorId.None, message, inner)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ActivityException"/> class.</summary>
        /// <param name="errorId">ActivityErrorId for the exception.</param>
        /// <param name="message">Message for the exception.</param>
        /// <param name="inner">The inner exception.</param>
        public ActivityException(ActivityErrorId errorId, string message, Exception inner)
            : base(message, inner)
        {
            this.ActivityErrorId = errorId;
        }

        /// <summary>Initializes a new instance of the <see cref="ActivityException"/> class.</summary>
        /// <param name="info">SerializationInfo object</param>
        /// <param name="context">StreamingContext object</param>
        protected ActivityException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
            // Deserialize our data from the serialized data
            this.ActivityErrorId = (ActivityErrorId)info.GetValue(ActivityErrorIdSerializationName, typeof(ActivityErrorId));
        }

        /// <summary>Gets or sets the activity error id for the exception.</summary>
        public ActivityErrorId ActivityErrorId { get; set; }

        /// <summary>Serialize the exception.</summary>
        /// <param name="info">SerializationInfo object</param>
        /// <param name="context">StreamingContext object</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            // Add our values to the serialized data
            info.AddValue(ActivityErrorIdSerializationName, this.ActivityErrorId, typeof(ActivityErrorId));
        }
    }
}