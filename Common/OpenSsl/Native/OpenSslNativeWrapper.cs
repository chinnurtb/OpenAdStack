//-----------------------------------------------------------------------
// <copyright file="OpenSslNativeWrapper.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Utilities.Runtime.InteropServices;

namespace Utilities.Cryptography.OpenSsl.Native
{
    /// <summary>Base class for native wrappers of OpenSSL resources</summary>
    internal abstract class OpenSslNativeWrapper : NativeWrapper<IOpenSslInterop, OpenSslInterop32, OpenSslInterop64>
    {
        /// <summary>Initializes a new instance of the OpenSslNativeWrapper class</summary>
        /// <param name="handle">Handle to the wrapped resource</param>
        public OpenSslNativeWrapper(IntPtr handle)
            : base(handle)
        {
        }
    }
}
