// -----------------------------------------------------------------------
// <copyright file="NameAttribute.cs" company="Emerging Media Group">
//    Copyright Emerging Media Group. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Activities
{
    /// <summary>
    /// Attribute specifying the name of the activity
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class NameAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the NameAttribute class.
        /// </summary>
        /// <param name="value">The name</param>
        public NameAttribute(string value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets the name of the activity
        /// </summary>
        public string Value { get; private set; }
    }
}
