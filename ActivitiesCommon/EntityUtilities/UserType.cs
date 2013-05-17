//Copyright 2012-2013 Rare Crowds, Inc.
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

namespace EntityUtilities
{
    /// <summary>Types of users</summary>
    public enum UserType
    {
        /// <summary>Unknown user type</summary>
        Unknown,

        /// <summary>Stand-alone user</summary>
        /// <remarks>Logs into rarecrowds.com directly via ACS</remarks>
        StandAlone,

        /// <summary>AppNexus App user</summary>
        /// <remarks>Logs in via AppNexus console single-sign-on</remarks>
        AppNexusApp,

        /// <summary>Default user type (stand-alone)</summary>
        Default = StandAlone,
    }
}
