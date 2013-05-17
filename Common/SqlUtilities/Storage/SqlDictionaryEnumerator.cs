//-----------------------------------------------------------------------
// <copyright file="SqlDictionaryEnumerator.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SqlUtilities.Storage
{
    /// <summary>
    /// IEnumerator implementation for enumerating SqlDictionary
    /// </summary>
    /// <typeparam name="TValue">Value type of the SqlDictionary</typeparam>
    internal class SqlDictionaryEnumerator<TValue> :
        IEnumerator<KeyValuePair<string, TValue>>,
        IEnumerator,
        IDisposable
    {
        /// <summary>The SqlDictionary to enumerate</summary>
        private SqlDictionary<TValue> dictionary;

        /// <summary>The keys to be enumerated</summary>
        private string[] keys;

        /// <summary>The current position of the enumerator</summary>
        private int position;

        /// <summary>Whether the enumerator has been disposed</summary>
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the SqlDictionaryEnumerator class.
        /// </summary>
        /// <param name="dictionary">The dictionary to enumerate</param>
        public SqlDictionaryEnumerator(SqlDictionary<TValue> dictionary)
        {
            this.keys = dictionary.Keys.ToArray();
            this.dictionary = dictionary;
            this.disposed = false;
            this.position = -1;
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        public KeyValuePair<string, TValue> Current
        {
            get
            {
                this.CheckDisposed();
                var key = this.keys[this.position];
                return new KeyValuePair<string, TValue>(key, this.dictionary[key]);
            }
        }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        object IEnumerator.Current
        {
            get
            {
                return ((IEnumerator<KeyValuePair<string, TValue>>)this).Current;
            }
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// True if the enumerator was successfully advanced to the next element;
        /// false if the enumerator has passed the end of the collection.
        /// </returns>
        public bool MoveNext()
        {
            this.CheckDisposed();
            if (this.position + 1 < this.keys.Length)
            {
                this.position += 1;
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element
        /// in the collection.
        /// </summary>
        public void Reset()
        {
            this.CheckDisposed();
            this.position = -1;
        }

        /// <summary>Disposes the enumerator</summary>
        public void Dispose()
        {
            if (!this.disposed)
            {
                this.disposed = true;
                this.dictionary = null;
                this.keys = null;
            }
        }

        /// <summary>
        /// Check if the enumerator is disposed and
        /// if so throw an ObjectDisposedException.
        /// </summary>
        private void CheckDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.ToString());
            }
        }
    }
}
