// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LogOff.aspx.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Threading;
using ConfigManager;
using Microsoft.IdentityModel.Web;

namespace WebLayer
{
    /// <summary>
    /// Class to manage logging off
    /// </summary>
    public partial class LogOff : System.Web.UI.Page
    {
        /// <summary>
        /// Event that occurs when the page loads
        /// </summary>
        /// <param name="sender">sender of this event</param>
        /// <param name="e">event arguments</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // Erase session cookies
            WSFederationAuthenticationModule authModule = FederatedAuthentication.WSFederationAuthenticationModule;
            authModule.SignOut(true);

            // Pause for 5 seconds so the user can see the message on the page. (Message is that the user will be logged off)
            Thread.Sleep(5000);

            Response.Redirect(Config.GetValue("WL.WLIDLogOffUrl"), true);
        }
    }
}