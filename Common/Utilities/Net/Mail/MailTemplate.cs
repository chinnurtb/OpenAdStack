// -----------------------------------------------------------------------
// <copyright file="MailTemplate.cs" company="Rare Crowds Inc">
// Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Runtime.Serialization;

namespace Utilities.Net.Mail
{    
    /// <summary>Template for generating mail</summary>
    [DataContract]
    public class MailTemplate
    {
        /// <summary>Gets or sets sender</summary>
        [DataMember]
        public string Sender { get; set; }

        /// <summary>Gets or sets subject format</summary>
        [DataMember]
        public string SubjectFormat { get; set; }
        
        /// <summary>Gets or sets body format</summary>
        [DataMember]
        public string BodyFormat { get; set; }

        /// <summary>Gets or sets a value indicating whether the body should be treated as HTML</summary>
        [DataMember]
        public bool IsBodyHtml { get; set; }
    }
}
