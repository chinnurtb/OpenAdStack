//-----------------------------------------------------------------------
// <copyright file="QueryResult.cs" company="Rare Crowds Inc.">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>
    /// Helper class to navigate a jagged result set from a sql query
    /// </summary>
    public class QueryResult
    {
        /// <summary>Jagged table to hold the query results.</summary>
        private readonly IList<IList<QueryRecord>> recordSets = new List<IList<QueryRecord>>();

        /// <summary>Determine if the result set is empty.</summary>
        /// <returns>True if the result set is empty.</returns>
        public bool IsEmpty()
        {
            return !this.recordSets.SelectMany(r => r).Any();
        }

        /// <summary>Add a record set to the result.</summary>
        /// <returns>The index of the record set that was added.</returns>
        public int AddRecordSet()
        {
            this.recordSets.Add(new List<QueryRecord>());
            return this.recordSets.Count - 1;
        }

        /// <summary>Add a record to the given record set.</summary>
        /// <param name="record">The record to add.</param>
        /// <param name="recordSetIndex">The index of the record set to which the record should be added.</param>
        public void AddRecord(QueryRecord record, int recordSetIndex)
        {
            if (this.recordSets.Count < recordSetIndex)
            {
                throw new DataAccessException("Unexpected Sql Result. There are not {0} record sets in query result."
                    .FormatInvariant(recordSetIndex));
            }

            this.recordSets[recordSetIndex].Add(record);
        }

        /// <summary>Get a single record from the single result set.</summary>
        /// <returns>The record.</returns>
        public QueryRecord GetSingleRecord()
        {
            return this.GetSingleRecord(0, 0);
        }

        /// <summary>Get a single record from the result sets.</summary>
        /// <param name="recordSetIndex">The expected index of the record set.</param>
        /// <param name="recordIndex">The desired record index within the record set.</param>
        /// <returns>The record.</returns>
        public QueryRecord GetSingleRecord(int recordSetIndex, int recordIndex)
        {
            if (this.recordSets.Count <= recordSetIndex)
            {
                throw new DataAccessException("Unexpected Sql Result. There are not {0} record sets in query result."
                    .FormatInvariant(recordSetIndex + 1));
            }

            var recordSet = this.recordSets[recordSetIndex];
            if (recordSet.Count <= recordIndex)
            {
                throw new DataAccessException("Unexpected Sql Result. There are not {0} records in the record set."
                    .FormatInvariant(recordIndex + 1));
            }

            return recordSet[recordIndex];
        }

        /// <summary>Get all the records from the single result set.</summary>
        /// <returns>The records.</returns>
        public IList<QueryRecord> GetRecords()
        {
            return this.GetRecords(0);
        }

        /// <summary>Get all the records from the result sets.</summary>
        /// <param name="recordSetIndex">The expected index of the record set.</param>
        /// <returns>The records.</returns>
        public IList<QueryRecord> GetRecords(int recordSetIndex)
        {
            if (this.recordSets.Count <= recordSetIndex)
            {
                throw new DataAccessException("Unexpected Sql Result. There are not {0} record sets in query result."
                    .FormatInvariant(recordSetIndex + 1));
            }

            return this.recordSets[recordSetIndex];
        }

        /// <summary>Get the index of a record matching a given field value.</summary>
        /// <typeparam name="T">The type of the field value.</typeparam>
        /// <param name="recordSetIndex">The index of the record set.</param>
        /// <param name="fieldToMatch">The field name to match.</param>
        /// <param name="valueToMatch">The value to match.</param>
        /// <returns>The index of the matching record.</returns>
        public QueryRecord GetMatchingRecord<T>(int recordSetIndex, string fieldToMatch, T valueToMatch)
        {
            if (this.recordSets.Count < recordSetIndex)
            {
                throw new DataAccessException("Unexpected Sql Result. There are not {0} record sets in query result."
                    .FormatInvariant(recordSetIndex));
            }

            var matchingRecords = this.recordSets[recordSetIndex].Where(r => r.Match(fieldToMatch, valueToMatch)).ToList();
            
            if (!matchingRecords.Any())
            {
                throw new DataAccessException("Unexpected Sql Result. No records found matching {0} = {1}"
                    .FormatInvariant(fieldToMatch, valueToMatch));
            }

            if (matchingRecords.Count() > 1)
            {
                throw new DataAccessException("Unexpected Sql Result. Multiple records found matching {0} = {1}"
                    .FormatInvariant(fieldToMatch, valueToMatch));
            }

            return matchingRecords.First();
        }
    }
}