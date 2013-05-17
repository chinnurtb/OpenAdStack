// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ErrorResponse.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApiLayer
{
    /// <summary>
    /// This class holds error data that is sent to client 
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Gets or sets external error id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets error message 
        /// </summary>
        public string Message { get; set; }              
    }
}