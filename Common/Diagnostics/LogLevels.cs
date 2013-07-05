//-----------------------------------------------------------------------
// <copyright file="LogLevels.cs" company="Rare Crowds Inc">
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
