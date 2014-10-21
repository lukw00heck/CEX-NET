﻿using System;

namespace VTDev.Projects.CEX.CryptoGraphic
{
    /// <summary>
    /// Implements Cipher FeedBack (CFB) mode
    /// </summary>
    public class CFB : ICipherMode
    {
        #region Fields
        private IBlockCipher _blockCipher;
        private int _blockSize = 16;
        private byte[] _cfbIv;
        private byte[] _cfbBuffer;
        private int _feedbackSize = 8;
        private bool _isEncryption = false;
        #endregion

        #region Properties
        /// <summary>
        /// Unit block size of internal cipher.
        /// </summary>
        public int BlockSize
        {
            get { return _blockCipher.BlockSize; }
        }

        /// <summary>
        /// Feedback size
        /// </summary>
        public int FeedBackSize
        {
            get { return _feedbackSize; }
            set { _feedbackSize = value; }
        }

        /// <summary>
        /// Used as encryptor, false for decryption. 
        /// </summary>
        public bool IsEncryption
        {
            get { return _isEncryption; }
            private set { _isEncryption = value; }
        }

        /// <summary>
        /// Cipher name
        /// </summary>
        public string Name
        {
            get { return "CFB"; }
        }

        /// <summary>
        /// Intitialization Vector
        /// </summary>
        public byte[] Vector
        {
            get { return _cfbIv; }
            set
            {
                if (value == null)
                    throw new ArgumentOutOfRangeException("Invalid IV! IV can not be null.");
                if (value.Length != this.BlockSize)
                    throw new ArgumentOutOfRangeException("Invalid IV size! Valid size must be equal to cipher blocksize.");

                int vectorSize = value.Length;
                _cfbIv = new byte[vectorSize];
                _cfbBuffer = new byte[vectorSize];

                Buffer.BlockCopy(value, 0, _cfbIv, 0, vectorSize);
            }
        }

        #endregion

        #region Constructor
        /// <summary>
        /// Initialize the Cipher
        /// </summary>
        /// <param name="Transform">Underlying encryption algorithm</param>
        public CFB(IBlockCipher Transform)
        {
            _blockCipher = Transform;
            _blockSize = this.BlockSize;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Initialize the Cipher
        /// </summary>
        /// <param name="Encryptor">Cipher is used for encryption, false to decrypt</param>
        /// <param name="Transform">Underlying encryption engine</param>
        /// <param name="Key">Cipher key</param>
        /// <param name="Vector">Must be equal to engine blocksize</param>
        public void Init(bool Encryptor, byte[] Key, byte[] Vector)
        {
            _blockCipher.Init(true, Key);
            this.Vector = Vector;
            this.IsEncryption = Encryptor;
        }

        /// <summary>
        /// Transform a block of bytes.
        /// Init must be called before this method can be used.
        /// </summary>
        /// <param name="Input">Bytes to Encrypt/Decrypt</param>
        /// <param name="Output">Encrypted or Decrypted bytes</param>
        public void Transform(byte[] Input, byte[] Output)
        {
            if (this.IsEncryption)
                EncryptBlock(Input, Output);
            else
                DecryptBlock(Input, Output);
        }

        /// <summary>
        /// Transform a block of bytes within an array.
        /// Init must be called before this method can be used.
        /// </summary>
        /// <param name="Input">Bytes to Encrypt</param>
        /// <param name="InOffset">Offset with the Input array</param>
        /// <param name="Output">Encrypted bytes</param>
        /// <param name="OutOffset">Offset with the Output array</param>
        public void Transform(byte[] Input, int InOffset, byte[] Output, int OutOffset)
        {
            if (this.IsEncryption)
                EncryptBlock(Input, InOffset, Output, OutOffset);
            else
                DecryptBlock(Input, InOffset, Output, OutOffset);
        }

        /// <summary>
        /// Decrypt a single block of bytes.
        /// </summary>
        /// <param name="Input">Encrypted bytes</param>
        /// <param name="Output">Decrypted bytes</param>
        public void DecryptBlock(byte[] Input, byte[] Output)
        {
            // decrypt input
            _blockCipher.EncryptBlock(_cfbIv, _cfbBuffer);

            // copy forward iv
            Buffer.BlockCopy(_cfbIv, _feedbackSize, _cfbIv, 0, _cfbIv.Length - _feedbackSize);
            Buffer.BlockCopy(Input, 0, _cfbIv, _cfbIv.Length - _feedbackSize, _feedbackSize);

            // xor output and iv
            for (int i = 0; i < _feedbackSize; i++)
                Output[i] = (byte)(_cfbBuffer[i] ^ Input[i]);
        }

        /// <summary>
        /// Decrypt a block of bytes within an array.
        /// </summary>
        /// <param name="Input">Encrypted bytes</param>
        /// <param name="InOffset">Offset with the Input array</param>
        /// <param name="Output">Decrypted bytes</param>
        /// <param name="OutOffset">Offset with the Output array</param>
        public void DecryptBlock(byte[] Input, int InOffset, byte[] Output, int OutOffset)
        {
            // decrypt input
            _blockCipher.EncryptBlock(_cfbIv, _cfbBuffer);

            // copy forward iv
            Buffer.BlockCopy(_cfbIv, _feedbackSize, _cfbIv, 0, _cfbIv.Length - _feedbackSize);
            Buffer.BlockCopy(Input, InOffset, _cfbIv, _cfbIv.Length - _feedbackSize, _feedbackSize);

            // xor output and iv
            for (int i = 0; i < _feedbackSize; i++)
                Output[i + OutOffset] = (byte)(_cfbBuffer[i] ^ Input[i + InOffset]);
        }

        /// <summary>
        /// Encrypt a block of bytes.
        /// </summary>
        /// <param name="Input">Bytes to Encrypt</param>
        /// <param name="Output">Encrypted bytes</param>
        public void EncryptBlock(byte[] Input, byte[] Output)
        {
            // encrypt iv
            _blockCipher.EncryptBlock(_cfbIv, _cfbBuffer);

            for (int i = 0; i < _feedbackSize; i++)
                Output[i] = (byte)(_cfbBuffer[i] ^ Input[i]);

            // copy output to iv
            Buffer.BlockCopy(_cfbIv, _feedbackSize, _cfbIv, 0, _cfbIv.Length - _feedbackSize);
            Buffer.BlockCopy(Output, 0, _cfbIv, _cfbIv.Length - _feedbackSize, _feedbackSize);
        }

        /// <summary>
        /// Encrypt a block of bytes within an array.
        /// </summary>
        /// <param name="Input">Bytes to Encrypt</param>
        /// <param name="InOffset">Offset with the Input array</param>
        /// <param name="Output">Encrypted bytes</param>
        /// <param name="OutOffset">Offset with the Output array</param>
        public void EncryptBlock(byte[] Input, int InOffset, byte[] Output, int OutOffset)
        {
            // encrypt iv
            _blockCipher.EncryptBlock(_cfbIv, _cfbBuffer);

            for (int i = 0; i < _feedbackSize; i++)
                Output[OutOffset + i] = (byte)(_cfbBuffer[i] ^ Input[InOffset + i]);

            // copy output to iv
            Buffer.BlockCopy(_cfbIv, _feedbackSize, _cfbIv, 0, _cfbIv.Length - _feedbackSize);
            Buffer.BlockCopy(Output, OutOffset, _cfbIv, _cfbIv.Length - _feedbackSize, _feedbackSize);
        }

        /// <summary>
        /// Dispose of this class
        /// </summary>
        public void Dispose()
        {
            if (_blockCipher != null)
                _blockCipher.Dispose();
            if (_cfbIv != null)
                Array.Clear(_cfbIv, 0, _cfbIv.Length);
            if (_cfbBuffer != null)
                Array.Clear(_cfbBuffer, 0, _cfbBuffer.Length);
        }
        #endregion
    }
}