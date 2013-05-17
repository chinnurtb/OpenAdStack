//-----------------------------------------------------------------------
// <copyright file="LogLevels.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Diagnostics
{
    /// <summary>Log Level for log messages</summary>
    [Flags]
    public enum LogLevels
    {
        /// <summary>Default level</summary>
        None = 0x00,

        /// <summary>Trace level</summary>
        Trace = 0x01,

        /// <summary>Information level</summary>
        Information = 0x02,

        /// <summary>Warning level</summary>
        Warning = 0x04,

        /// <summary>Error level</summary>
        Error = 0x08,

        /// <summary>All levels</summary>
        All = Trace | Information | Warning | Error
    }
}
