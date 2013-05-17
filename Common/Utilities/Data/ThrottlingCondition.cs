//---------------------------------------------------------------------------------------
// Microsoft Windows Server AppFabric Customer Advisory Team (CAT) Best Practices Series
//
// This sample is supplemental to the technical guidance published on the community
// blog at http://blogs.msdn.com/appfabriccat/.
//
//---------------------------------------------------------------------------------------
// <copyright file="ThrottlingCondition.cs" company="Microsoft Corporation">
// Copyright © 2011 Microsoft Corporation. All rights reserved.
// </copyright>
//
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER
// EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF
// MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE. YOU BEAR THE RISK OF USING IT.
//---------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.AppFabricCat.Samples.Azure.TransientFaultHandling.SqlAzure
{
    /// <summary>
    /// Defines the possible throttling modes in SQL Database.
    /// </summary>
    public enum ThrottlingMode
    {
        /// <summary>
        /// Corresponds to "No Throttling" throttling mode whereby all SQL statements can be processed.
        /// </summary>
        NoThrottling = 0,

        /// <summary>
        /// Corresponds to "Reject Update / Insert" throttling mode whereby SQL statements such as INSERT, UPDATE, CREATE TABLE and CREATE INDEX are rejected.
        /// </summary>
        RejectUpdateInsert = 1,

        /// <summary>
        /// Corresponds to "Reject All Writes" throttling mode whereby SQL statements such as INSERT, UPDATE, DELETE, CREATE, DROP are rejected.
        /// </summary>
        RejectAllWrites = 2,

        /// <summary>
        /// Corresponds to "Reject All" throttling mode whereby all SQL statements are rejected.
        /// </summary>
        RejectAll = 3,

        /// <summary>
        /// Corresponds to an unknown throttling mode whereby the throttling mode cannot be determined with certainty.
        /// </summary>
        Unknown = -1
    }

    /// <summary>
    /// Defines the possible throttling types in SQL Database.
    /// </summary>
    public enum ThrottlingType
    {
        /// <summary>
        /// Indicates that no throttling was applied to a given resource.
        /// </summary>
        None = 0,

        /// <summary>
        /// Corresponds to a Soft throttling type. Soft throttling is applied when machine resources such as, Cpu, IO, storage, and worker threads exceed 
        /// predefined safety thresholds despite the load balancer’s best efforts. 
        /// </summary>
        Soft = 1,

        /// <summary>
        /// Corresponds to a Hard throttling type. Hard throttling is applied when the machine is out of resources, for example storage space.
        /// With hard throttling, no new connections are allowed to the databases hosted on the machine until resources are freed up.
        /// </summary>
        Hard = 2,

        /// <summary>
        /// Corresponds to an unknown throttling type in the event that the throttling type cannot be determined with certainty.
        /// </summary>
        Unknown = 3
    }

    /// <summary>
    /// Defines the types of resources in SQL Database which may be subject to throttling conditions.
    /// </summary>
    public enum ThrottledResourceType
    {
        /// <summary>
        /// Corresponds to "Physical Database Space" resource which may be subject to throttling.
        /// </summary>
        PhysicalDatabaseSpace = 0,

        /// <summary>
        /// Corresponds to "Physical Log File Space" resource which may be subject to throttling.
        /// </summary>
        PhysicalLogSpace = 1,

        /// <summary>
        /// Corresponds to "Transaction Log Write IO Delay" resource which may be subject to throttling.
        /// </summary>
        LogWriteIODelay = 2,

        /// <summary>
        /// Corresponds to "Database Read IO Delay" resource which may be subject to throttling.
        /// </summary>
        DataReadIODelay = 3,

        /// <summary>
        /// Corresponds to "Cpu" resource which may be subject to throttling.
        /// </summary>
        Cpu = 4,

        /// <summary>
        /// Corresponds to "Database Size" resource which may be subject to throttling.
        /// </summary>
        DatabaseSize = 5,

        /// <summary>
        /// Corresponds to "SQL Worker Thread Pool" resource which may be subject to throttling.
        /// </summary>
        WorkerThreads = 7,

        /// <summary>
        /// Corresponds to an internal resource which may be subject to throttling.
        /// </summary>
        Internal = 6,

        /// <summary>
        /// Corresponds to an unknown resource type in the event that the actual resource cannot be determined with certainty.
        /// </summary>
        Unknown = -1
    }

    /// <summary>
    /// Implements an object holding the decoded reason code returned from SQL Database when encountering throttling conditions.
    /// </summary>
    public class ThrottlingCondition
    {
        /// <summary>
        /// Returns the error number that corresponds to throttling conditions reported by SQL Database.
        /// </summary>
        public const int ThrottlingErrorNumber = 40501;

        /// <summary>
        /// Provides a compiled regular expression used for extracting the reason code from the error message.
        /// </summary>
        private static readonly Regex sqlErrorCodeRegEx = new Regex(@"Code:\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Maintains a collection of key-value pairs where a key is the resource type and a value is the type of throttling applied to the given resource type.
        /// </summary>
        private IList<Tuple<ThrottledResourceType, ThrottlingType>> throttledResources = new List<Tuple<ThrottledResourceType, ThrottlingType>>(9);

        /// <summary>
        /// Gets an unknown throttling condition in the event the actual throttling condition cannot be determined with certainty.
        /// </summary>
        public static ThrottlingCondition Unknown
        {
            get
            {
                var unknownCondition = new ThrottlingCondition() { ThrottlingMode = ThrottlingMode.Unknown };
                unknownCondition.throttledResources.Add(Tuple.Create<ThrottledResourceType, ThrottlingType>(ThrottledResourceType.Unknown, ThrottlingType.Unknown));

                return unknownCondition;
            }
        }

        /// <summary>
        /// Gets the value that reflects the throttling mode in SQL Database.
        /// </summary>
        public ThrottlingMode ThrottlingMode { get; private set; }

        /// <summary>
        /// Gets the list of resources in SQL Database that were subject to throttling conditions.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1006", Justification = "sic")]
        public IEnumerable<Tuple<ThrottledResourceType, ThrottlingType>> ThrottledResources
        {
            get { return this.throttledResources; }
        }

        /// <summary>
        /// Gets a value indicating whether physical data file space throttling was reported by SQL Database.
        /// </summary>
        public bool IsThrottledOnDataSpace
        {
            get { return this.throttledResources.Where(x => x.Item1 == ThrottledResourceType.PhysicalDatabaseSpace).Count() > 0; }
        }

        /// <summary>
        /// Gets a value indicating whether physical log space throttling was reported by SQL Database.
        /// </summary>
        public bool IsThrottledOnLogSpace
        {
            get { return this.throttledResources.Where(x => x.Item1 == ThrottledResourceType.PhysicalLogSpace).Count() > 0; }
        }

        /// <summary>
        /// Gets a value indicating whether transaction activity throttling was reported by SQL Database.
        /// </summary>
        public bool IsThrottledOnLogWrite
        {
            get { return this.throttledResources.Where(x => x.Item1 == ThrottledResourceType.LogWriteIODelay).Count() > 0; }
        }

        /// <summary>
        /// Gets a value indicating whether data read activity throttling was reported by SQL Database.
        /// </summary>
        public bool IsThrottledOnDataRead
        {
            get { return this.throttledResources.Where(x => x.Item1 == ThrottledResourceType.DataReadIODelay).Count() > 0; }
        }

        /// <summary>
        /// Gets a value indicating whether Cpu throttling was reported by SQL Database.
        /// </summary>
        public bool IsThrottledOnCpu
        {
            get { return this.throttledResources.Where(x => x.Item1 == ThrottledResourceType.Cpu).Count() > 0; }
        }

        /// <summary>
        /// Gets a value indicating whether database size throttling was reported by SQL Database.
        /// </summary>
        public bool IsThrottledOnDatabaseSize
        {
            get { return this.throttledResources.Where(x => x.Item1 == ThrottledResourceType.DatabaseSize).Count() > 0; }
        }

        /// <summary>
        /// Gets a value indicating whether concurrent requests throttling was reported by SQL Database.
        /// </summary>
        public bool IsThrottledOnWorkerThreads
        {
            get { return this.throttledResources.Where(x => x.Item1 == ThrottledResourceType.WorkerThreads).Count() > 0; }
        }

        /// <summary>
        /// Gets a value indicating whether throttling conditions could not be determined with certainty.
        /// </summary>
        public bool IsUnknown
        {
            get { return ThrottlingMode == ThrottlingMode.Unknown; }
        }

        /// <summary>
        /// Determines throttling conditions from the specified SQL exception.
        /// </summary>
        /// <param name="ex">The <see cref="SqlException"/> object containing information relevant to an error returned by SQL Server when encountering throttling conditions.</param>
        /// <returns>An instance of the object holding the decoded reason codes returned from SQL Database upon encountering throttling conditions.</returns>
        public static ThrottlingCondition FromException(SqlException ex)
        {
            if (ex != null)
            {
                foreach (SqlError error in ex.Errors)
                {
                    if (error.Number == ThrottlingErrorNumber)
                    {
                        return FromError(error);
                    }
                }
            }

            return Unknown;
        }

        /// <summary>
        /// Determines the throttling conditions from the specified SQL error.
        /// </summary>
        /// <param name="error">The <see cref="SqlError"/> object containing information relevant to a warning or error returned by SQL Server.</param>
        /// <returns>An instance of the object holding the decoded reason codes returned from SQL Database when encountering throttling conditions.</returns>
        public static ThrottlingCondition FromError(SqlError error)
        {
            if (error != null)
            {
                var match = sqlErrorCodeRegEx.Match(error.Message);
                int reasonCode = 0;

                if (match.Success && int.TryParse(match.Groups[1].Value, out reasonCode))
                {
                    return FromReasonCode(reasonCode);
                }
            }

            return Unknown;
        }

        /// <summary>
        /// Determines the throttling conditions from the specified reason code.
        /// </summary>
        /// <param name="reasonCode">The reason code returned by SQL Database which contains the throttling mode and the exceeded resource types.</param>
        /// <returns>An instance of the object holding the decoded reason codes returned from SQL Database when encountering throttling conditions.</returns>
        public static ThrottlingCondition FromReasonCode(int reasonCode)
        {
            if (reasonCode > 0)
            {
                // Decode throttling mode from the last 2 bits.
                ThrottlingMode throttlingMode = (ThrottlingMode)(reasonCode & 3);

                var condition = new ThrottlingCondition() { ThrottlingMode = throttlingMode };

                // Shift 8 bits to truncate throttling mode.
                int groupCode = reasonCode >> 8;

                // Determine throttling type for all well-known resources that may be subject to throttling conditions.
                condition.throttledResources.Add(Tuple.Create<ThrottledResourceType, ThrottlingType>(ThrottledResourceType.PhysicalDatabaseSpace, (ThrottlingType)(groupCode & 3)));
                condition.throttledResources.Add(Tuple.Create<ThrottledResourceType, ThrottlingType>(ThrottledResourceType.PhysicalLogSpace, (ThrottlingType)((groupCode = groupCode >> 2) & 3)));
                condition.throttledResources.Add(Tuple.Create<ThrottledResourceType, ThrottlingType>(ThrottledResourceType.LogWriteIODelay, (ThrottlingType)((groupCode = groupCode >> 2) & 3)));
                condition.throttledResources.Add(Tuple.Create<ThrottledResourceType, ThrottlingType>(ThrottledResourceType.DataReadIODelay, (ThrottlingType)((groupCode = groupCode >> 2) & 3)));
                condition.throttledResources.Add(Tuple.Create<ThrottledResourceType, ThrottlingType>(ThrottledResourceType.Cpu, (ThrottlingType)((groupCode = groupCode >> 2) & 3)));
                condition.throttledResources.Add(Tuple.Create<ThrottledResourceType, ThrottlingType>(ThrottledResourceType.DatabaseSize, (ThrottlingType)((groupCode = groupCode >> 2) & 3)));
                condition.throttledResources.Add(Tuple.Create<ThrottledResourceType, ThrottlingType>(ThrottledResourceType.Internal, (ThrottlingType)((groupCode = groupCode >> 2) & 3)));
                condition.throttledResources.Add(Tuple.Create<ThrottledResourceType, ThrottlingType>(ThrottledResourceType.WorkerThreads, (ThrottlingType)((groupCode = groupCode >> 2) & 3)));
                condition.throttledResources.Add(Tuple.Create<ThrottledResourceType, ThrottlingType>(ThrottledResourceType.Internal, (ThrottlingType)((groupCode = groupCode >> 2) & 3)));

                return condition;
            }
            else
            {
                return Unknown;
            }
        }

        /// <summary>
        /// Returns a textual representation of the current ThrottlingCondition object including the information held with respect to throttled resources.
        /// </summary>
        /// <returns>A string that represents the current ThrottlingCondition object.</returns>
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            result.AppendFormat("Mode: {0} | ", ThrottlingMode);

            var resources = this.throttledResources
                .Where(x => x.Item1 != ThrottledResourceType.Internal)
                .Select(x => string.Format(CultureInfo.InvariantCulture, "{0}: {1}", x.Item1, x.Item2))
                .OrderBy(x => x)
                .ToArray();

            result.Append(string.Join(", ", resources));

            return result.ToString();
        }
    }
}
