//-----------------------------------------------------------------------
// <copyright file="IKeyContainer.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Utilities.Cryptography
{
    /// <summary>Interface for key containers used with ICipherEngine</summary>
    public interface IKeyContainer
    {
        /// <summary>Gets the contained key</summary>
        object Key { get; }
    }
}
