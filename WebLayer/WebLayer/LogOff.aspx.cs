// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LogOff.aspx.cs" company="Rare Crowds Inc">
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
            Response.Cookies.Clear(); 

            // Pause for 3 seconds so the user can see the message on the page. (Message is that the user will be logged off)
            Thread.Sleep(3000);

            Response.Redirect(Config.GetValue("WL.WLIDLogOffUrl"), true);
        }
    }
}