//-----------------------------------------------------------------------
// <copyright file="HistoryElement.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataAccessLayer;

namespace DynamicAllocationActivities
{
    /// <summary>
    /// Class to represent an item in the history index
    /// </summary>
    public class HistoryElement
    {
        /// <summary>
        /// Gets or sets the AllocationStartTime
        /// </summary>
        public string AllocationStartTime { get; set; }

        /// <summary>
        /// Gets or sets the AllocationOutputs entity id
        /// </summary>
        public string AllocationOutputsId { get; set; }
    }
}
