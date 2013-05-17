// -----------------------------------------------------------------------
// <copyright file="SourceNameAttribute.cs" company="Emerging Media Group">
//    Copyright Emerging Media Group. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace ScheduledActivities
{
    /// <summary>Attribute specifying the name of the scheduled activity source</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class SourceNameAttribute : Attribute
    {
        /// <summary>Initializes a new instance of the SourceNameAttribute class.</summary>
        /// <param name="value">The name</param>
        public SourceNameAttribute(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException("value");
            }

            this.Value = value.ToLowerInvariant();
        }

        /// <summary>Gets the name</summary>
        public string Value { get; private set; }
    }
}
