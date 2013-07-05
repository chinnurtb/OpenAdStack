//-----------------------------------------------------------------------
// <copyright file="NativeBuffer.cs" company="Rare Crowds Inc">
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
using System.Runtime.InteropServices;
using System.Text;

namespace Utilities.Runtime.InteropServices
{
    /// <summary>Wrapper for unmanaged buffers</summary>
    public class NativeBuffer : NativeWrapper<object, object, object>
    {
        /// <summary>
        /// Initializes a new instance of the NativeBuffer class and allocates
        /// an unmanaged buffer of <paramref name="size"/> bytes.
        /// </summary>
        /// <param name="size">Size of the unmanaged buffer to allocate</param>
        public NativeBuffer(int size)
            : base(Marshal.AllocHGlobal(size))
        {
        }

        /// <summary>
        /// Initializes a new instance of the NativeBuffer class, allocates an
        /// unmanaged buffer and copies the provided bytes into it.
        /// </summary>
        /// <param name="value">Bytes to be copied to the unmanaged buffer</param>
        public NativeBuffer(byte[] value)
            : this(value.Length)
        {
            Marshal.Copy(value, 0, this.Handle, value.Length);
        }

        /// <summary>
        /// Initializes a new instance of the NativeBuffer class, allocates an
        /// unmanaged buffer and copies the provided text into it using the
        /// specified character encoding.
        /// </summary>
        /// <param name="text">Text to be copied to the unmanaged buffer</param>
        /// <param name="encoding">The encoding to be used</param>
        public NativeBuffer(string text, Encoding encoding)
            : this(encoding.GetBytes(text))
        {
        }

        /// <summary>
        /// Initializes a new instance of the NativeBuffer class, allocates an
        /// unmanaged buffer and copies the provided text into it as UTF-8 bytes
        /// </summary>
        /// <param name="text">Text to be copied to the unmanaged buffer</param>
        public NativeBuffer(string text)
            : this(text, Encoding.UTF8)
        {
        }

        /// <summary>Frees the unmanaged buffer</summary>
        protected override void OnDispose()
        {
            Marshal.FreeHGlobal(this.Handle);
        }
    }
}
