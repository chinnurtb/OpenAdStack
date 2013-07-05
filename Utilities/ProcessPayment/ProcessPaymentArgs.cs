// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProcessPaymentArgs.cs" company="Rare Crowds Inc">
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

using System.IO;
using ConsoleAppUtilities;
using DataAccessLayer;

namespace ProcessPayment
{
    /// <summary>Command-line argument helpers for ProcessPayment</summary>
    public class ProcessPaymentArgs : CommandLineArguments
    {
        /// <summary>Gets or sets company entity id.</summary>
        [CommandLineArgument("-company", "Company Entity Id.")]
        public string CompanyEntityId { get; set; }

        /// <summary>Gets or sets campaign entity id.</summary>
        [CommandLineArgument("-campaign", "Campaign Entity Id.")]
        public string CampaignEntityId { get; set; }

        /// <summary>Gets or sets user id.</summary>
        [CommandLineArgument("-uid", "User Id.")]
        public string UserId { get; set; }

        /// <summary>Gets or sets a value indicating whether to force billing test approval of a campaign without a billing charge check.</summary>
        [CommandLineArgument("-ta", "Force test approval flag. Only campaign id required.")]
        public bool IsForceTestApprove { get; set; }

        /// <summary>Gets or sets a value indicating whether to force billing approval of a campaign without a billing charge check.</summary>
        [CommandLineArgument("-a", "Force approval flag. Valid User Id required and logged but access not checked.")]
        public bool IsForceApprove { get; set; }

        /// <summary>Gets or sets a value indicating whether to submit a billing charge/refund.</summary>
        [CommandLineArgument("-c", "Charge flag.")]
        public bool IsCharge { get; set; }

        /// <summary>Gets or sets a value indicating whether to submit a billing charge check (charge/refund check).</summary>
        [CommandLineArgument("-q", "Charge Check flag. Amount must be positive.")]
        public bool IsCheck { get; set; }

        /// <summary>Gets or sets a value indicating whether to output billing history only.</summary>
        [CommandLineArgument("-h", "History dump flag.")]
        public bool IsHistoryDump { get; set; }

        /// <summary>Gets or sets the amount of the payment to process (negative for refund).</summary>
        [CommandLineArgument("-amount", "The amount (negative for refund).")]
        public string Amount { get; set; }

        /// <summary>Gets or sets the charge id for a refund.</summary>
        [CommandLineArgument("-cid", "Required for refund. The charge id of the original charge.")]
        public string ChargeId { get; set; }

        /// <summary>Gets or sets the log file</summary>
        [CommandLineArgument("-log", "Log file path.")]
        public FileInfo LogFile { get; set; }

        /// <summary>Gets or sets a value indicating whether to submit a billing refund.</summary>
        public bool IsRefund { get; set; }

        /// <summary>Gets a value indicating whether the arguments are valid</summary>
        public override bool ArgumentsValid
        {
            get
            {
                if (!(this.IsForceTestApprove ^ this.IsForceApprove ^ this.IsHistoryDump ^ this.IsCharge ^ this.IsCheck))
                {
                    return false;
                }

                if (!EntityId.IsValidEntityId(this.CampaignEntityId))
                {
                    return false;
                }

                if (this.IsForceTestApprove)
                {
                    return true;
                }

                if (string.IsNullOrEmpty(this.UserId))
                {
                    return false;
                }

                if (this.IsForceApprove)
                {
                    return true;
                }

                if (!EntityId.IsValidEntityId(this.CompanyEntityId))
                {
                    return false;
                }

                if (this.IsHistoryDump)
                {
                    return true;
                }

                if (string.IsNullOrEmpty(this.Amount))
                {
                    return false;
                }

                decimal amount;
                if (!decimal.TryParse(this.Amount, out amount))
                {
                    return false;
                }

                if (amount < 0m)
                {
                    if (this.IsCheck)
                    {
                        return false;
                    }

                    this.IsRefund = true;
                    if (string.IsNullOrEmpty(this.ChargeId))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}