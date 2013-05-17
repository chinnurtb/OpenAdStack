// -----------------------------------------------------------------------
// <copyright file="ElementBy.cs" company="Emerging Media Group">
// Copyright Emerging Media Group. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace SeleniumFramework.Utilities
{
    /// <summary>
    /// This enum contains members to find element by Id, Css and XPath
    /// </summary>
    public enum ElementBy
    {
        /// <summary>
        /// No Element 
        /// </summary>
        None,

        /// <summary>
        /// Element Type recognized by ID
        /// </summary>
        Id,

        /// <summary>
        /// Element Type recognized by XPath
        /// </summary>
        XPath,

        /// <summary>
        /// Element Type recognized by Css
        /// </summary>
        Css,

        /// <summary>
        /// Element Type recognized by Name
        /// </summary>
        Name
    }
}
