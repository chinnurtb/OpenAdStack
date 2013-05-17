//-----------------------------------------------------------------------
// <copyright file="IMailClient.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
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
