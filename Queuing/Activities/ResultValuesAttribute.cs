﻿//-----------------------------------------------------------------------
// <copyright file="ResultValuesAttribute.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Activities
{
    /// <summary>
    /// Attribute specifying what values are required in the ActivityRequest
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class ResultValuesAttribute : Attribute
    {
        /// <summary>
        /// List of the names of the required values
        /// </summary>
        private string[] valueNames;

        /// <summary>
        /// Initializes a new instance of the ResultValuesAttribute class.
        /// </summary>
        /// <param name="valueNames">Names of the required values</param>
        public ResultValuesAttribute(params string[] valueNames)
        {
            this.valueNames = valueNames;
        }

        /// <summary>
        /// Gets the names of the required values
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Okay to suppress for attributes: http://msdn.microsoft.com/en-us/library/0fss9skc.aspx")]
        public string[] ValueNames
        {
            get
            {
                return this.valueNames.ToArray();
            }
        }
    }
}