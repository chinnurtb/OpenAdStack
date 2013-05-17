//-----------------------------------------------------------------------
// <copyright file="BlobExtensions.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.WindowsAzure.StorageClient;

namespace AzureUtilities.Storage
{
    /// <summary>Extensions for CloudBlobs</summary>
    public static class BlobExtensions
    {
        /// <summary>Checks whether or not a blob exists</summary>
        /// <param name="blob">Blob to check the existence of</param>
        /// <returns>True if the blob exists; otherwise, false.</returns>
        public static bool Exists(this CloudBlob blob)
        {
            try
            {
                blob.FetchAttributes();
                return true;
            }
            catch (StorageClientException sce)
            {
                if (sce.ErrorCode == StorageErrorCode.ResourceNotFound)
                {
                    return false;
                }

                throw;
            }
        }
    }
}
