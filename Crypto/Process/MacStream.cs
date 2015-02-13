﻿#region Directives
using System;
using System.IO;
using VTDev.Libraries.CEXEngine.Crypto.Digest;
using VTDev.Libraries.CEXEngine.Crypto.Mac;
#endregion

#region License Information
// The MIT License (MIT)
// 
// Copyright (c) 2015 John Underhill
// This file is part of the CEX Cryptographic library.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// 
// Written by John Underhill, January 22, 2015
// contact: develop@vtdev.com
#endregion

namespace VTDev.Libraries.CEXEngine.Crypto.Process
{
    /// <summary>
    /// <h3>MAC stream helper class.</h3>
    /// 
    /// <list type="bullet">
    /// <item><description>Uses any of the implemented <see cref="Macs">Macs</see> using the <see cref="IMac">interface</see>.</description></item>
    /// <item><description>Mac must be fully initialized before passed to the constructor.</description></item>
    /// <item><description>Mac can be Disposed when this class is <see cref="Dispose()">Disposed</see>, set the DisposeEngine parameter in the class Constructor to true to dispose automatically.</description></item>
    /// <item><description>Input Stream can be Disposed when this class is Disposed, set the DisposeStream parameter in the <see cref="Initialize(Stream, bool)"/> call to true to dispose automatically.</description></item>
    /// <item><description>Implementation has a Progress counter that returns total sum of bytes processed per either <see cref="ComputeMac(long, long)">ComputeMac([InOffset], [OutOffset])</see> calls.</description></item>
    /// </list>
    /// </summary> 
    /// 
    /// <example>
    /// <description>Example of hashing a Stream:</description>
    /// <code>
    /// using (IMac mac = new SHA512HMAC())
    /// {
    ///     mac.Initialize(new KeyParams(Key));
    ///     
    ///     using (MacStream mstrm = new MacStream(mac, [false]))
    ///     {
    ///         // assign the input stream
    ///         mstrm.Initialize(InputStream, [true]);
    ///         // get the digest
    ///         (byte[]) hash = mstrm.ComputeMac([Length], [InOffset]);
    ///     }
    /// }
    /// </code>
    /// </example>
    /// 
    /// <revisionHistory>
    ///     <revision date="2015/01/23" version="1.3.0.0" author="John Underhill">Initial release</revision>
    /// </revisionHistory>
    /// 
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Digest">VTDev.Libraries.CEXEngine.Crypto.Digest Namespace</seealso>
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Digest.IDigest">VTDev.Libraries.CEXEngine.Crypto.Digest.IDigest Interface</seealso>
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Digests">VTDev.Libraries.CEXEngine.Crypto.Digests Enumeration</seealso>
    public sealed class MacStream : IDisposable
    {
        #region Events
        /// <summary>
        /// Progress indicator delegate
        /// </summary>
        /// 
        /// <param name="sender">Event owner object</param>
        /// <param name="e">Progress event arguments containing percentage and bytes processed as the UserState param</param>
        public delegate void ProgressDelegate(object sender, System.ComponentModel.ProgressChangedEventArgs e);

        /// <summary>
        /// Progress Percent Event; returns bytes processed as an integer percentage
        /// </summary>
        public event ProgressDelegate ProgressPercent;
        #endregion

        #region Fields
        private int _blockSize;
        private IMac _macEngine;
        private bool _disposeEngine = false;
        private bool _disposeStream = false;
        private Stream _inStream;
        private bool _isDisposed = false;
        private bool _isInitialized = false;
        private long _progressInterval;
        #endregion

        #region Constructor
        /// <summary>
        /// Initialize the class.
        /// <para>Mac must be fully initialized, including key, before calling this method.</para>
        /// </summary>
        /// 
        /// <param name="Mac">The initialized <see cref="IMac"/> instance</param>
        /// <param name="DisposeEngine">Dispose of digest engine when <see cref="Dispose()"/> on this class is called</param>
        /// 
        /// <exception cref="System.ArgumentNullException">Thrown if a null <see cref="IDigest">Digest</see> is used</exception>
        /// <exception cref="System.ArgumentException">Thrown if an uninitialized Mac is used</exception>
        public MacStream(IMac Mac, bool DisposeEngine = false)
        {
            if (Mac == null)
                throw new ArgumentNullException("The Mac can not be null!");
            if (!Mac.IsInitialized)
                throw new ArgumentException("The Mac has not been initialized!");

            _macEngine = Mac;
            _blockSize = _macEngine.BlockSize;
            _disposeEngine = DisposeEngine;
        }

        /// <summary>
        /// Finalize objects
        /// </summary>
        ~MacStream()
        {
            Dispose(false);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize internal state
        /// </summary>
        /// 
        /// <param name="InStream">The Source stream to be transformed</param>
        /// <param name="DisposeStream">Dispose of streams when <see cref="Dispose()"/> on this class is called</param>
        /// 
        /// <exception cref="System.ArgumentNullException">Thrown if a null Input stream is used</exception>
        public void Initialize(Stream InStream, bool DisposeStream = false)
        {
            if (InStream == null)
                throw new ArgumentNullException("The Input stream can not be null!");

            _disposeStream = DisposeStream;
            _inStream = InStream;
            CalculateInterval(_inStream.Length);
            _isInitialized = true;
        }

        /// <summary>
        /// Process the entire length of the Input Stream
        /// </summary>
        /// 
        /// <returns>The Message Authentication Code</returns>
        ///  
        /// <exception cref="System.InvalidOperationException">Thrown if ComputeMac is called before Initialize()</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if Size + Offset is longer than Input stream</exception>
        public byte[] ComputeMac()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Initialize() must be called before a write operation can be performed!");
            if (_inStream.Length < 1)
                throw new ArgumentOutOfRangeException("The Input stream is too short!");

            return Compute(_inStream.Length);
        }

        /// <summary>
        /// Process a length within the Input stream using an Offset
        /// </summary>
        /// 
        /// <param name="Length">The number of bytes to process</param>
        /// <param name="Offset">The Input Stream positional offset</param>
        /// 
        /// <returns>The Message Authentication Code</returns>
        /// 
        /// <exception cref="System.InvalidOperationException">Thrown if ComputeHash is called before Initialize()</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if Size + Offset is longer than Input stream</exception>
        public byte[] ComputeMac(long Length, long Offset)
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Initialize() must be called before a ComputeMac operation can be performed!");
            if (Length - Offset < 1)
                throw new ArgumentOutOfRangeException("The Input stream is too short!");
            if (Length - Offset > _inStream.Length)
                throw new ArgumentOutOfRangeException("The Input stream is too short!");

            CalculateInterval(Length);
            _inStream.Position = Offset;//35291

            return Compute(Length);
        }
        #endregion

        #region Private Methods
        private void CalculateInterval(long Offset)
        {
            long interval = (_inStream.Length - Offset) / 100;

            if (interval < _blockSize)
                _progressInterval = _blockSize;
            else
                _progressInterval = interval - (interval % _blockSize);

            if (_progressInterval == 0)
                _progressInterval = _blockSize;
        }

        private void CalculateProgress(long Size, bool Completed = false)
        {
            if (Completed || Size % _progressInterval == 0)
            {
                if (ProgressPercent != null)
                {
                    double progress = 100.0 * (double)Size / _inStream.Length;
                    ProgressPercent(this, new System.ComponentModel.ProgressChangedEventArgs((int)progress, (object)Size));
                }
            }
        }

        private byte[] Compute(long Length)
        {
            int bytesRead = 0;
            long bytesTotal = 0;
            byte[] buffer = new byte[_blockSize];
            byte[] chkSum = new byte[_macEngine.DigestSize];
            long maxBlocks = Length / _blockSize;

            for (int i = 0; i < maxBlocks; i++)
            {
                bytesRead = _inStream.Read(buffer, 0, _blockSize);
                _macEngine.BlockUpdate(buffer, 0, bytesRead);
                bytesTotal += bytesRead;
                CalculateProgress(bytesTotal);
            }

            // last block
            if (bytesTotal < Length)
            {
                buffer = new byte[Length - bytesTotal];
                bytesRead = _inStream.Read(buffer, 0, buffer.Length);
                _macEngine.BlockUpdate(buffer, 0, buffer.Length);
                bytesTotal += buffer.Length;
            }

            // get the hash
            _macEngine.DoFinal(chkSum, 0);
            CalculateProgress(bytesTotal);

            return chkSum;
        }
        #endregion

        #region IDispose
        /// <summary>
        /// Dispose of this class
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool Disposing)
        {
            if (!_isDisposed && Disposing)
            {
                try
                {
                    if (_disposeEngine)
                    {
                        if (_macEngine != null)
                        {
                            _macEngine.Dispose();
                            _macEngine = null;
                        }
                    }
                    if (_disposeStream)
                    {
                        if (_inStream != null)
                        {
                            _inStream.Dispose();
                            _inStream = null;
                        }
                    }
                }
                catch { }

                _isDisposed = true;
            }
        }
        #endregion
    }
}