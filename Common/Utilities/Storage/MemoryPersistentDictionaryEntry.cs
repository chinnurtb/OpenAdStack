//-----------------------------------------------------------------------
// <copyright file="MemoryPersistentDictionaryEntry.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Utilities.Storage
{
    /// <summary>
    /// Store entry consisting of a byte array, ETag and metadata
    /// </summary>
    internal class MemoryPersistentDictionaryEntry : IPersistentDictionaryEntry
    {
        /// <summary>Byte array for the entry contents</summary>
        private byte[] content;

        /// <summary>ETag for the store entry</summary>
        private string etag;

        /// <summary>Whether the content bytes are compressed</summary>
        private bool compressed;

        /// <summary>
        /// Initializes a new instance of the MemoryPersistentDictionaryEntry class.
        /// </summary>
        public MemoryPersistentDictionaryEntry()
        {
            this.etag = Guid.NewGuid().ToString("N");
            this.content = new byte[0];
        }

        /// <summary>Gets the ETag for the store entry</summary>
        public string ETag
        {
            get { return this.etag; }
        }

        /// <summary>Reads the content bytes from the entry</summary>
        /// <returns>The bytes</returns>
        public byte[] ReadAllBytes()
        {
            lock (this)
            {
                var bytes = new byte[this.content.Length];
                this.content.CopyTo(bytes, 0);
                return this.compressed ? bytes.Inflate() : bytes;
            }
        }

        /// <summary>Writes the content bytes to the entry</summary>
        /// <param name="bytes">The bytes</param>
        /// <param name="compress">Whether to compress the content</param>
        public void WriteAllBytes(byte[] bytes, bool compress)
        {
            lock (this)
            {
                this.content = new byte[bytes.Length];
                bytes.CopyTo(this.content, 0);
                this.content = compress ? this.content.Deflate() : this.content;
                this.compressed = compress;
                this.UpdateETag();
            }
        }

        /// <summary>Gets a string representation of the entry</summary>
        /// <returns>A string representing the entry</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "ToString should never throw")]
        public override string ToString()
        {
            var content = string.Empty;
            try
            {
                using (var reader = new StreamReader(new MemoryStream(this.ReadAllBytes())))
                {
                    content = reader.ReadToEnd();
                }
            }
            catch
            {
            }

            return "[{0}]{1}".FormatInvariant(this.ETag, content);
        }

        /// <summary>Updates the entry's ETag.</summary>
        /// <remarks>
        /// This should be done after any changes are made to the entry by
        /// either writing to its stream or changing its metadata values.
        /// </remarks>
        private void UpdateETag()
        {
            this.etag = Guid.NewGuid().ToString("N");
        }
    }
}