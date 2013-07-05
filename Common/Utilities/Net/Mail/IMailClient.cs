//-----------------------------------------------------------------------
// <copyright file="IMailClient.cs" company="Rare Crowds Inc">
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

namespace Utilities.Net.Mail
{
    /// <summary>Interface for mail clients</summary>
    public interface IMailClient
    {
        /// <summary>
        /// Sends an email created from the provided template and values
        /// </summary>
        /// <param name="mailTemplateName">Mail template name</param>
        /// <param name="recipient">Recipient address</param>
        /// <param name="subjectArgs">Values for the formatted subject</param>
        /// <param name="bodyArgs">Values for the formatted body</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// No mail template matching <paramref name="mailTemplateName"/> was found.
        /// </exception>
        /// <exception cref="System.FormatException">
        /// An error occured formatting the message subject and/or body.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// An error occured sending the message.
        /// </exception>
        void SendMail(string mailTemplateName, string recipient, object[] subjectArgs, object[] bodyArgs);
    }
}
