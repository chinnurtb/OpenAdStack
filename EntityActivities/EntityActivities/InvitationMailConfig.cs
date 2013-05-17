// -----------------------------------------------------------------------
// <copyright file="InvitationMailConfig.cs" company="Emerging Media Group">
// Copyright Emerging Media Group. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Runtime.Serialization;

namespace EntityActivities
{    
    /// <summary>
    /// Represents invitation mail configuration
    /// </summary>
    [DataContract]
    public class InvitationMailConfig
    {
        /// <summary>
        /// Gets or sets subject format for invitation email
        /// </summary>
        [DataMember]
        public string SubjectFormat { get; set; }
        
        /// <summary>
        /// Gets or sets body format for invitation email
        /// </summary>
        [DataMember]
        public string BodyFormat { get; set; }
    }
}
