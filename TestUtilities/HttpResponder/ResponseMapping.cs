//-----------------------------------------------------------------------
// <copyright file="ResponseMapping.cs" company="Rare Crowds Inc">
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

using System.Runtime.Serialization;

namespace HttpResponder
{
    /// <summary>
    /// Represents response mapping
    /// </summary>
    [DataContract]
    public class ResponseMapping
    {
        /// <summary>
        /// Gets or sets name of embedded resource
        /// </summary>
        [DataMember(IsRequired = true)]
        public string ResourceName { get; set; }

        /// <summary>
        /// Gets or sets Http verbs like GET, PUT, etc.
        /// </summary>
        [DataMember(IsRequired = true)]
        public string HttpVerb { get; set; }

        /// <summary>
        /// Gets or sets Response status code
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int ResponseStatusCode { get; set; }

        /// <summary>
        /// Gets or sets url contains
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056", Justification = "This is UrlContains not a uri, So cannot be convert it to System.Uri")]
        [DataMember(EmitDefaultValue = false)]
        public string UrlContains { get; set; }

        /// <summary>
        /// Gets or sets url Regex
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056", Justification = "This is urlRegex not a uri, So cannot be convert it to System.Uri")]
        [DataMember(EmitDefaultValue = false)]
        public string UrlRegex { get; set; }

        /// <summary>
        /// Gets or sets headers Contains
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string HeadersContains { get; set; }

        /// <summary>
        /// Gets or sets headers Regex
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string HeadersRegex { get; set; }

        /// <summary>
        /// Gets or sets body contains
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string BodyContains { get; set; }

        /// <summary>
        /// Gets or sets body regex
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string BodyRegex { get; set; }
    }
}