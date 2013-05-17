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

namespace DataServiceUtilities
{
    /// <summary>
    /// Requestable resultSet formats for data service activities
    /// </summary>
    /// <remarks>
    /// The schema of the data within the results varies between
    /// different data services as appropriate to their consumers.
    /// </remarks>
    public enum DataServiceResultsFormat
    {
        /// <summary>
        /// JSON formatted list
        /// </summary>
        Json,

        /// <summary>
        /// XML representation of the results
        /// </summary>
        Xml,
    }
}
