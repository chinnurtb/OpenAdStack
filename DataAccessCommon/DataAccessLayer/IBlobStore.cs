// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IBlobStore.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace DataAccessLayer
{
    /// <summary>Interface for accessing Blob stores independent of underlying technology.</summary>
    public interface IBlobStore
    {
        /// <summary>Gets a storage key factory for this blob store.</summary>
        /// <returns>An IStorageKeyFactory</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Factory method.")]
        IStorageKeyFactory GetStorageKeyFactory();

        /// <summary>Get a blob entity given a storage key.</summary>
        /// <param name="key">An IStorageKey key.</param>
        /// <returns>A blob entity that is not deserialized.</returns>
        IRawEntity GetBlobByKey(IStorageKey key);

        /// <summary>Save an entity in the entity store.</summary>
        /// <param name="rawEntity">The raw entity.</param>
        /// <param name="company">The company (for storage auditing).</param>
        void SaveBlob(IRawEntity rawEntity, string company);
    }
}
