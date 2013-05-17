//-----------------------------------------------------------------------
// <copyright file="AbstractPersistentDictionary.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Xml;
using ConfigManager;
using Diagnostics;

namespace Utilities.Storage
{
    /// <summary>Abstract base class for IPersistentDictionary implementations</summary>
    /// <typeparam name="TValue">Value type. Must be a valid data contract</typeparam>
    public abstract class AbstractPersistentDictionary<TValue> :
        IPersistentDictionary<TValue>,
        IDictionary<string, TValue>,
        ICollection<KeyValuePair<string, TValue>>,
        IEnumerable<KeyValuePair<string, TValue>>,
        IEnumerable
    {
        /// <summary>Default compression threshold bytes</summary>
        private const int DefaultCompressionThresholdBytes = 1024;

        /// <summary>Whether or not to perform serialization on read/write</summary>
        private bool raw;

        /// <summary>Initializes a new instance of the AbstractPersistentDictionary class.</summary>
        /// <param name="raw">Whether or not to skip serialization. Only valid for <typeparamref name="TValue"/> of byte[]</param>
        /// <exception cref="System.Runtime.Serialization.InvalidDataContractException">
        /// The type specified by <typeparamref name="TValue"/> is not a data contract.
        /// </exception>
        /// <seealso cref="System.Runtime.Serialization.DataContract"/>
        protected AbstractPersistentDictionary(bool raw)
        {
            if (raw && typeof(TValue) != typeof(byte[]))
            {
                throw new ArgumentException(
                    "Raw mode is only valid for TValue of byte[] (not '{0}')"
                    .FormatInvariant(typeof(TValue).FullName),
                    "raw");
            }

            this.raw = raw;
            this.ETags = new Dictionary<string, string>();
            this.Serializer = new DataContractSerializer(typeof(TValue));
        }

        /// <summary>Gets the contents</summary>
        public ICollection<TValue> Values
        {
            get { return new List<TValue>(this.Select(kvp => kvp.Value)); }
        }

        /// <summary>Gets a value indicating whether this is read-only.</summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>Gets the name of the store for this dictionary</summary>
        public abstract string StoreName { get; }

        /// <summary>Gets the number of elements</summary>
        public abstract int Count { get; }

        /// <summary>Gets the names of the value entries</summary>
        public abstract ICollection<string> Keys { get; }

        /// <summary>Gets the size threshold for compression</summary>
        internal static int CompressionThresholdBytes
        {
            get
            {
                try
                {
                    return Config.GetIntValue("PersistentDictionary.CompressionThresholdBytes");
                }
                catch (ArgumentException)
                {
                    return DefaultCompressionThresholdBytes;
                }
            }
        }

        /// <summary>Gets the dictionary of ETags for the entries</summary>
        protected Dictionary<string, string> ETags { get; private set; }

        /// <summary>Gets the serializer for entry values</summary>
        protected DataContractSerializer Serializer { get; private set; }

        /// <summary>Gets or sets the value entry for the given key</summary>
        /// <param name="key">Key for the value entry</param>
        /// <returns>The value</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// The property is retrieved and an entry with the specified key is not found.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// The property is set and the value is null.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// The property is set and an error occurs writing the bytes of the entry.
        /// </exception>
        /// <exception cref="Utilities.Storage.InvalidETagException">
        /// The property is set and the entry's ETag has changed since the
        /// value was last retrieved. To clear this condition the value must
        /// be retrieved again before it can be set.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "All exceptions thrown by IPersistentDictionaryEntry.WriteAllBytes other than InvalidEtagException are wrapped in System.IO.IOExceptions")]
        public TValue this[string key]
        {
            get
            {
                lock (this)
                {
                    var entry = this.GetEntry(key, true, false, true);

                    // Raw byte arrays bypass serialization
                    if (this.raw && typeof(TValue) == typeof(byte[]))
                    {
                        return (TValue)(dynamic)entry.ReadAllBytes();
                    }

                    // Deserialize the value from the entry
                    var retries = 5;
                    while (true)
                    {
                        var entryBytes = entry.ReadAllBytes();
                        if (entryBytes.Length == 0)
                        {
                            LogManager.Log(
                                LogLevels.Warning,
                                "Cannot deserialize empty entry '{0}' from persisted dictionary '{1}'. The entry will be removed.",
                                key,
                                this.StoreName);
                            this.Remove(key);
                            return default(TValue);
                        }

                        using (var stream = new MemoryStream(entryBytes))
                        {
                            using (var reader = new XmlTextReader(new StreamReader(stream, Encoding.UTF8)))
                            {
                                try
                                {
                                    return (TValue)this.Serializer.ReadObject(reader);
                                }
                                catch (SerializationException se)
                                {
                                    if (retries-- > 0)
                                    {
                                        Thread.Sleep(10);
                                        continue;
                                    }

                                    LogManager.Log(
                                        LogLevels.Warning,
                                        "Error deserializing entry '{0}' from persisted dictionary '{1}'. The entry will be removed.\n{2}",
                                        key,
                                        this.StoreName,
                                        se);
                                    this.Remove(key);
                                    return default(TValue);
                                }
                            }
                        }
                    }
                }
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                lock (this)
                {
                    var entry = this.GetEntry(key, false, true, false);

                    // Raw byte arrays bypass serialization
                    if (this.raw && typeof(TValue) == typeof(byte[]))
                    {
                        var bytes = (byte[])(dynamic)value;
                        entry.WriteAllBytes(bytes, bytes.Length >= CompressionThresholdBytes);
                        this.ETags[key] = entry.ETag;
                        return;
                    }

                    // Serialize the value to the entry
                    using (var stream = new MemoryStream())
                    {
                        using (var writer = new XmlTextWriter(new StreamWriter(stream, Encoding.UTF8)))
                        {
                            try
                            {
                                this.Serializer.WriteObject(writer, value);
                            }
                            catch (SerializationException se)
                            {
                                LogManager.Log(
                                    LogLevels.Warning,
                                    "Error serializing entry '{0}' to persisted dictionary '{1}'. The entry will be removed.\n{2}",
                                    key,
                                    this.StoreName,
                                    se);
                                this.Remove(key);
                                this.ETags.Remove(key);
                                throw;
                            }
                        }

                        stream.Flush();
                        var bytes = stream.ToArray();

                        try
                        {
                            entry.WriteAllBytes(bytes, bytes.Length >= CompressionThresholdBytes);
                        }
                        catch (InvalidETagException)
                        {
                            throw;
                        }
                        catch (Exception e)
                        {
                            var exception = new IOException(
                                "An error occured while writing entry '{0}' of '{1}'"
                                .FormatInvariant(key, this.StoreName),
                                e);
                            LogManager.Log(
                                LogLevels.Error,
                                exception.Message + "\n{0}",
                                exception);
                            throw exception;
                        }
                    }

                    this.ETags[key] = entry.ETag;
                }
            }
        }

        /// <summary>Adds a value entry with the specified key</summary>
        /// <param name="key">The entry key</param>
        /// <param name="value">The entry value</param>
        /// <exception cref="System.ArgumentNullException">The entry is null</exception>
        /// <exception cref="System.ArgumentException">An entry with the same key already exists</exception>
        public void Add(string key, TValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (this.ContainsKey(key))
            {
                throw new ArgumentException(
                    "A value already exists with the key '{0}'."
                    .FormatInvariant(key),
                    "value");
            }

            this[key] = value;
        }

        /// <summary>Adds a value entry from a KeyValuePair</summary>
        /// <param name="item">KeyValuePair containing the value</param>
        /// <exception cref="System.ArgumentNullException">The entry is null</exception>
        /// <exception cref="System.ArgumentException">An entry with the same key already exists</exception>
        public void Add(KeyValuePair<string, TValue> item)
        {
            this.Add(item.Key, item.Value);
        }

        /// <summary>Removes the entry with the specified key</summary>
        /// <param name="key">The key of the entry to remove</param>
        /// <returns>
        /// True if the entry is successfully removed; otherwise, false.
        /// Also returns false if the entry was not found.
        /// </returns>
        public bool Remove(string key)
        {
            lock (this)
            {
                this.ETags.Remove(key);
                return this.RemoveEntry(key);
            }
        }

        /// <summary>Removes all value entries</summary>
        public void Clear()
        {
            lock (this)
            {
                this.ETags.Clear();
                this.ClearEntries();
            }
        }

        /// <summary>Deletes the underlying store where the dictionary is persisted</summary>
        public void Delete()
        {
            lock (this)
            {
                this.ETags.Clear();
                this.DeleteStore();
            }
        }

        /// <summary>Gets the entry with the specified key</summary>
        /// <param name="key">The key of the entry to get</param>
        /// <param name="value">
        /// When this method returns, the entry value with the specified key, if the key is found;
        /// otherwise, null. This parameter is passed uninitialized.
        /// </param>
        /// <returns>True if an entry with the specified key is found; otherwise, false.</returns>
        public bool TryGetValue(string key, out TValue value)
        {
            if (!this.Keys.Contains(key))
            {
                value = default(TValue);
                return false;
            }

            value = this[key];
            return true;
        }

        /// <summary>Determines whether the dictionary contains an entry with the sepecified key</summary>
        /// <param name="key">The key of the value entry to locate</param>
        /// <exception cref="System.ArgumentNullException">Thrown if key is null</exception>
        /// <returns>True if an entry with the key is found; otherwise, false.</returns>
        public abstract bool ContainsKey(string key);

        /// <summary>Returns an enumerator that iterates through the entry value.</summary>
        /// <returns>An IEnumerator&lt;WorkItemScheduleEntry&gt; that can be used to iterate through the entry value.</returns>
        public abstract IEnumerator<KeyValuePair<string, TValue>> GetEnumerator();

        /// <summary>Returns an enumerate that iterates through the value entries</summary>
        /// <returns>An IEnumerator that can be used to iterate through the entries.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, TValue>>)this).GetEnumerator();
        }

        /// <summary>This method is not supported</summary>
        /// <param name="item">The parameter is not used.</param>
        /// <returns>No return</returns>
        /// <exception cref="System.NotImplementedException">Always thrown. This method is not supported.</exception>
        [SuppressMessage("Microsoft.Design", "CA1033", Justification = "Not implemented. Derived classes will be sealed")]
        bool ICollection<KeyValuePair<string, TValue>>.Contains(KeyValuePair<string, TValue> item)
        {
            throw new NotSupportedException();
        }

        /// <summary>Not Supported.</summary>
        /// <param name="array">The parameter is not used.</param>
        /// <param name="arrayIndex">The parameter is not used.</param>
        /// <exception cref="System.NotImplementedException">Always thrown. This method is not supported.</exception>
        [SuppressMessage("Microsoft.Design", "CA1033", Justification = "Not implemented. Derived classes will be sealed")]
        void ICollection<KeyValuePair<string, TValue>>.CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        /// <summary>This method is not supported</summary>
        /// <param name="item">The parameter is not used.</param>
        /// <returns>No return</returns>
        /// <exception cref="System.NotImplementedException">Always thrown. This method is not supported.</exception>
        bool ICollection<KeyValuePair<string, TValue>>.Remove(KeyValuePair<string, TValue> item)
        {
            throw new NotSupportedException();
        }

        /// <summary>Removes the entry with the specified key</summary>
        /// <param name="key">The key of the entry to remove</param>
        /// <returns>
        /// True if the entry is successfully removed; otherwise, false.
        /// Also returns false if the entry was not found.
        /// </returns>
        protected abstract bool RemoveEntry(string key);

        /// <summary>Removes all value entries</summary>
        protected abstract void ClearEntries();

        /// <summary>Deletes the underlying store where the dictionary is persisted</summary>
        protected abstract void DeleteStore();
        
        /// <summary>Gets the entry for the specified key</summary>
        /// <param name="key">Key to get the entry for</param>
        /// <param name="mustExist">Whether the entry must already exist</param>
        /// <param name="checkETag">Whether to check that the cached ETag is current</param>
        /// <param name="updateETag">Whether to update the cached ETag</param>
        /// <returns>The entry</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// <paramref name="mustExist"/> is true and no entry exists for the specified key
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="checkETag"/> is true and the ETag does not match the one cached
        /// </exception>
        protected abstract IPersistentDictionaryEntry GetEntry(string key, bool mustExist, bool checkETag, bool updateETag);
    }
}
