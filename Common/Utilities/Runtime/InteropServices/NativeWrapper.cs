//-----------------------------------------------------------------------
// <copyright file="NativeWrapper.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;

namespace Utilities.Runtime.InteropServices
{
    /// <summary>Base class for unmanaged type wrappers</summary>
    /// <typeparam name="TNativeInterop">Common base class/interface for the interop types</typeparam>
    /// <typeparam name="TNativeInterop32">32-bit interop implementation</typeparam>
    /// <typeparam name="TNativeInterop64">64-bit interop implementation</typeparam>
    [SuppressMessage("Microsoft.Design", "CA1005", Justification = "Having 3 type params is appropriate for this purpose")]
    public abstract class NativeWrapper<TNativeInterop, TNativeInterop32, TNativeInterop64> : IDisposable
        where TNativeInterop : class
        where TNativeInterop32 : TNativeInterop, new()
        where TNativeInterop64 : TNativeInterop, new()
    {
        /// <summary>Initializes static members of the NativeWrapper class</summary>
        static NativeWrapper()
        {
            Native = Environment.Is64BitProcess ?
                (TNativeInterop)new TNativeInterop64() :
                (TNativeInterop)new TNativeInterop32();
        }

        /// <summary>Initializes a new instance of the NativeWrapper class</summary>
        /// <param name="handle">Handle to the native resource</param>
        protected NativeWrapper(IntPtr handle)
        {
            this.Handle = handle;
        }

        /// <summary>Finalizes an instance of the NativeWrapper class</summary>
        ~NativeWrapper()
        {
            Dispose(false);
        }

        /// <summary>Gets the handle to the wrapped unmanaged resource</summary>
        public IntPtr Handle { get; private set; }

        /// <summary>Gets the native interop for the current environment</summary>
        protected static TNativeInterop Native { get; private set; }

        /// <summary>Frees the unmanaged buffer</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Frees the unmanaged buffer</summary>
        /// <param name="disposing">Whether to dispose of managed resources as well</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.Handle != IntPtr.Zero)
            {
                this.OnDispose();
                this.Handle = IntPtr.Zero;
            }
        }

        /// <summary>Dispose of the unmanaged resource</summary>
        protected abstract void OnDispose();
    }
}
