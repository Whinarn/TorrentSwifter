﻿using System;

namespace TorrentSwifter.Torrents
{
    /// <summary>
    /// A bit field.
    /// </summary>
    public sealed class BitField
    {
        #region Fields
        private readonly byte[] buffer;
        private readonly int length;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the underlying byte-buffer.
        /// </summary>
        public byte[] Buffer
        {
            get { return buffer; }
        }

        /// <summary>
        /// Gets the length of the bit field in bits.
        /// </summary>
        public int Length
        {
            get { return length; }
        }

        /// <summary>
        /// Gets the length of the bit field in bytes.
        /// </summary>
        public int ByteLength
        {
            get { return ((length + 7) >> 3); }
        }

        /// <summary>
        /// Gets or sets the bit at a specific index in the field.
        /// </summary>
        /// <param name="index">The bit index.</param>
        public bool this[int index]
        {
            get { return Get(index); }
            set { Set(index, value); }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new bit field.
        /// </summary>
        /// <param name="length">The length of the bit field in bits.</param>
        public BitField(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException("length");

            int byteLength = ((length + 7) >> 3);
            this.buffer = new byte[byteLength];
            this.length = length;
        }

        /// <summary>
        /// Creates a new bit field.
        /// </summary>
        /// <param name="buffer">The bit field byte-buffer.</param>
        /// <param name="length">The length of the bit field in bits.</param>
        public BitField(byte[] buffer, int length)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            else if (length < 0 || ((length + 7) >> 3) > buffer.Length)
                throw new ArgumentOutOfRangeException("length");

            this.buffer = buffer;
            this.length = length;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Returns the bit on a specific index field.
        /// </summary>
        /// <param name="index">The bit index.</param>
        /// <returns>The bit value.</returns>
        public bool Get(int index)
        {
            if (index < 0 || index >= length)
                throw new ArgumentOutOfRangeException("index");

            int byteIndex = (index >> 3);
            int bitIndex = (index & 7);
            return ((buffer[byteIndex] & (1 << bitIndex)) != 0);
        }

        /// <summary>
        /// Sets the bit on a specific index in the field.
        /// </summary>
        /// <param name="index">The bit index.</param>
        /// <param name="state">The bit value.</param>
        public void Set(int index, bool state)
        {
            int byteIndex = (index >> 3);
            int bitIndex = (index & 7);

            if (state)
            {
                buffer[byteIndex] |= (byte)(1 << bitIndex);
            }
            else
            {
                buffer[byteIndex] &= (byte)(~(1 << bitIndex));
            }
        }
        #endregion
    }
}
