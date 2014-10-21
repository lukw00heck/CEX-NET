﻿using System;
namespace VTDev.Projects.CEX.CryptoGraphic
{
    /// <summary>
    /// ECB mode (not recommended)
    /// </summary>
    public class ECB : ICipherMode
    {
        #region Fields
        private IBlockCipher _blockCipher;
        private int _blockSize = 16;
        private bool _isEncryption = false;
        private byte[] _nneVector;
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
            get { return "ECB"; }
        }

        /// <summary>
        /// Intitialization Vector
        /// </summary>
        public byte[] Vector
        {
            get { return _nneVector; }
            set { _nneVector = value; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Initialize the Cipher
        /// </summary>
        /// <param name="Transform">Underlying encryption algorithm</param>
        public ECB(IBlockCipher Transform)
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
        /// <param name="Vector">Vector can be null</param>
        public void Init(bool Encryptor, byte[] Key, byte[] Vector = null)
        {
            _blockCipher.Init(Encryptor, Key);
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
            _blockCipher.DecryptBlock(Input, Output);
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
            _blockCipher.DecryptBlock(Input, InOffset, Output, OutOffset);
        }

        /// <summary>
        /// Encrypt a block of bytes.
        /// </summary>
        /// <param name="Input">Bytes to Encrypt</param>
        /// <param name="Output">Encrypted bytes</param>
        public void EncryptBlock(byte[] Input, byte[] Output)
        {
            _blockCipher.EncryptBlock(Input, Output);
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
            _blockCipher.EncryptBlock(Input, InOffset, Output, OutOffset);
        }

        /// <summary>
        /// Dispose of this class
        /// </summary>
        public void Dispose()
        {
            if (_blockCipher != null)
                _blockCipher.Dispose();
        }
        #endregion
    }
}