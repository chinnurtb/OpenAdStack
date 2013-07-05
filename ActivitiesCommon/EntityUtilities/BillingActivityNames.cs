//-----------------------------------------------------------------------
// <copyright file="BillingActivityNames.cs" company="Rare Crowds Inc">
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

namespace EntityUtilities
{
    /// <summary>
    /// Names of entity properties used by Billing Activities
    /// </summary>
    public static class BillingActivityNames
    {
        /// <summary>Property name for Customer Billing Id.</summary>
        public const string CustomerBillingId = "CustomerBillingId";

        /// <summary>Property name for Billing History on campaign.</summary>
        public const string BillingHistory = "BillingHistory";

        /// <summary>Property name for flag indicating that billing is approved for campaign.</summary>
        public const string IsBillingApproved = "IsBillingApproved";

        /// <summary>Name for Default Payment Description.</summary>
        public const string DefaultPaymentDescription = "Rare Crowds Inc.";
    }
}
