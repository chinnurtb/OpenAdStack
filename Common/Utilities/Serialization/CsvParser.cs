//-----------------------------------------------------------------------
// <copyright file="CsvParser.cs" company="Rare Crowds Inc">
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace Utilities.Serialization
{
    /// <summary>Parser for CSV records</summary>
    [SuppressMessage("Microsoft.Naming", "CA1710", Justification = "Not really a collection")]
    public sealed class CsvParser : IEnumerable<IDictionary<string, string>>
    {
        /// <summary>Default value delimiter</summary>
        private const string DefaultDelimiter = ",";

        /// <summary>Default quote character</summary>
        private const char DefaultQuote = '"';

        /// <summary>Stream to parse</summary>
        private readonly Stream stream;

        /// <summary>Record value delimiter</summary>
        private readonly string delimiter;

        /// <summary>Quote character</summary>
        private readonly char quote;

        /// <summary>Initializes a new instance of the CsvParser class.</summary>
        /// <param name="stream">Stream to be parsed</param>
        /// <param name="delimiter">Record value delimiter</param>
        /// <param name="quote">Character for quoted values</param>
        private CsvParser(Stream stream, string delimiter, char quote)
        {
            this.delimiter = delimiter;
            this.stream = stream;
            this.quote = quote;
        }

        /// <summary>Parse CSV records from a stream</summary>
        /// <param name="stream">Stream to parse</param>
        /// <returns>Enumerator that parses the records</returns>
        [SuppressMessage("Microsoft.Design", "CA1006",
            Justification = "Nested sequence of dictionaries is appropriate")]
        public static IEnumerable<IDictionary<string, string>> Parse(
            Stream stream)
        {
            return Parse(stream, DefaultDelimiter);
        }

        /// <summary>Parse CSV records from text</summary>
        /// <param name="text">Text to parse</param>
        /// <returns>Enumerator that parses the records</returns>
        [SuppressMessage("Microsoft.Design", "CA1006",
            Justification = "Nested sequence of dictionaries is appropriate")]
        public static IEnumerable<IDictionary<string, string>> Parse(
            string text)
        {
            return Parse(text, DefaultDelimiter);
        }

        /// <summary>Parse CSV records from text</summary>
        /// <param name="text">Text to parse</param>
        /// <param name="delimiter">
        /// Record value delimiter (default is ",")
        /// </param>
        /// <returns>Enumerator that parses the records</returns>
        [SuppressMessage("Microsoft.Design", "CA1006",
            Justification = "Nested sequence of dictionaries is appropriate")]
        public static IEnumerable<IDictionary<string, string>> Parse(
            string text,
            string delimiter)
        {
            return Parse(new MemoryStream(Encoding.UTF8.GetBytes(text)), delimiter);
        }

        /// <summary>Parse CSV records from a stream</summary>
        /// <param name="stream">Stream to parse</param>
        /// <param name="delimiter">
        /// Record value delimiter (default is ",")
        /// </param>
        /// <returns>Enumerator that parses the records</returns>
        [SuppressMessage("Microsoft.Design", "CA1006",
            Justification = "Nested sequence of dictionaries is appropriate")]
        public static IEnumerable<IDictionary<string, string>> Parse(
            Stream stream,
            string delimiter)
        {
            return new CsvParser(stream, delimiter, DefaultQuote);
        }

        /// <summary>Gets the CSV parsing enumerator</summary>
        /// <returns>The enumerator</returns>
        public IEnumerator<IDictionary<string, string>> GetEnumerator()
        {
            return new CsvEnumerator(this.stream, this.delimiter, this.quote);
        }

        /// <summary>Gets the CSV parsing enumerator</summary>
        /// <returns>The enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<IDictionary<string, string>>)this).GetEnumerator();
        }

        /// <summary>Enumerator for parsing CSV records from a stream</summary>
        private sealed class CsvEnumerator : IEnumerator<IDictionary<string, string>>
        {
            /// <summary>Stream to enumerate</summary>
            private readonly Stream stream;

            /// <summary>Reader for the stream</summary>
            private readonly TextReader reader;
                
            /// <summary>Value delimiter</summary>
            private readonly string delimiter;

            /// <summary>Quote character</summary>
            private readonly char quoteChar;

            /// <summary>Quote string</summary>
            private readonly string quote;

            /// <summary>Escaped quote</summary>
            private readonly string escapedQuote;

            /// <summary>Record value headers</summary>
            private string[] headers;

            /// <summary>Initializes a new instance of the CsvEnumerator class.</summary>
            /// <param name="stream">Stream to enumerate</param>
            /// <param name="delimiter">Record value delimiter</param>
            /// <param name="quote">Character marking a quoted value</param>
            public CsvEnumerator(Stream stream, string delimiter, char quote)
            {
                if (stream == null)
                {
                    throw new ArgumentNullException("stream");
                }

                if (!stream.CanRead)
                {
                    throw new ArgumentException("Stream cannot be read", "stream");
                }

                if (!stream.CanSeek)
                {
                    throw new ArgumentException("Stream cannot seek", "stream");
                }

                this.stream = stream;
                this.stream.Seek(0, SeekOrigin.Begin);
                this.reader = new StreamReader(stream);

                this.delimiter = delimiter;
                this.quoteChar = quote;
                this.quote = this.quoteChar.ToString();
                this.escapedQuote = @"\" + this.quote;
            }

            /// <summary>Gets the current element in the collection.</summary>
            public IDictionary<string, string> Current { get; private set; }

            /// <summary>Gets the current element in the collection.</summary>
            object IEnumerator.Current
            {
                get { return ((IEnumerator<IDictionary<string, string>>)this).Current; }
            }

            /// <summary>Cleans up resources</summary>
            [SuppressMessage("Microsoft.Usage", "CA2213",
                Justification = "Disposing reader would dispose the underlying stream")]
            public void Dispose()
            {
            }

            /// <summary>Advances the enumerator to the next element of the collection.</summary>
            /// <returns>
            /// True if the enumerator was successfully advanced to the next element;
            /// False if the enumerator has passed the end of the collection.
            /// </returns>
            public bool MoveNext()
            {
                // Read the next row
                var row = this.reader.ReadLine();
                if (row == null)
                {
                    return false;
                }

                // If this is the first row, get the headers
                if (this.Current == null)
                {
                    this.headers = this.SplitAndTrim(row);
                    if (null == (row = this.reader.ReadLine()))
                    {
                        return false;
                    }
                }

                // Get the record values
                var values = this.SplitAndTrim(row);
                if (values.Length != this.headers.Length)
                {
                    var message =
                        "Headers/values count mismatch ({0} != {1}).\nHeaders: '{2}'\nValues: '{3}'"
                        .FormatInvariant(
                            this.headers.Length,
                            values.Length,
                            string.Join(",", this.headers),
                            string.Join(",", values));
                    throw new InvalidOperationException(message);
                }

                // Combine headers and values to create the record
                this.Current = this.headers
                    .Zip(values)
                    .ToDictionary(
                        pair => pair.Item1,
                        pair => pair.Item2);

                return true;
            }

            /// <summary>Resets the enumerator to its initial position</summary>
            public void Reset()
            {
                this.Current = null;
                this.stream.Seek(0, SeekOrigin.Begin);
            }

            /// <summary>Splits and trims a string of values</summary>
            /// <param name="valuesText">String containing values</param>
            /// <returns>The values split and trimmed</returns>
            private string[] SplitAndTrim(string valuesText)
            {
                // If the text did not contain any quotes then
                // split, trim and return the values as an array
                if (!valuesText.Contains(this.quote))
                {
                    return valuesText
                        .Split(new[] { this.delimiter }, StringSplitOptions.None)
                        .Select(value => value.Trim())
                        .ToArray();
                }

                // If the text contained quotes then the values
                // between quotes need to be rejoined
                var values = new List<string>();
                var rawValues = valuesText
                        .Split(new[] { this.delimiter }, StringSplitOptions.None);
                for (int i = 0; i < rawValues.Length; i++)
                {
                    string value;
                    if (!rawValues[i].Trim().StartsWith(this.quote, StringComparison.Ordinal))
                    {
                        // Not a quoted value
                        value = rawValues[i].Trim();
                    }
                    else
                    {
                        // Gather raw values until the closing quote is
                        // found or the end of the raw values is reached.
                        var quotedValues = new List<string>();
                        do
                        {
                            quotedValues.Add(rawValues[i]);

                            if (rawValues[i].Trim().EndsWith(this.quote, StringComparison.Ordinal) &&
                                !rawValues[i].Trim().EndsWith(this.escapedQuote, StringComparison.Ordinal))
                            {
                                break;
                            }
                        }
                        while (++i < rawValues.Length);

                        // Join the gathered raw values,
                        // trim whitespace and quotes and
                        // unescape escaped quotes
                        value =
                            string.Join(this.delimiter, quotedValues)
                            .Trim()
                            .Trim(this.quoteChar)
                            .Replace(this.escapedQuote, this.quote);
                    }

                    values.Add(value.Trim());
                }

                return values.ToArray();
            }
        }
    }
}
