#region License
/*
MIT License

Copyright (c) 2018 Mattias Edlund

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion

using System;

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
            int bitIndex = (7 - (index & 7));
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
            int bitIndex = (7 - (index & 7));

            if (state)
            {
                buffer[byteIndex] |= (byte)(1 << bitIndex);
            }
            else
            {
                buffer[byteIndex] &= (byte)(~(1 << bitIndex));
            }
        }

        /// <summary>
        /// Copies this bit field to another bit field.
        /// </summary>
        /// <param name="bitField">The destination bit field.</param>
        public void CopyTo(BitField bitField)
        {
            if (bitField == null)
                throw new ArgumentNullException("bitField");
            else if (length != bitField.length)
                throw new ArgumentException("The destination bit field has a different length.", "bitField");

            int byteLength = ByteLength;
            System.Buffer.BlockCopy(buffer, 0, bitField.buffer, 0, byteLength);
        }

        /// <summary>
        /// Gets if all bits are set in this bit field.
        /// </summary>
        /// <returns>If all bits are set.</returns>
        public bool HasAllSet()
        {
            bool result = true;
            for (int index = 0; index < length; index++)
            {
                int byteIndex = (index >> 3);
                int bitIndex = (7 - (index & 7));
                if (((buffer[byteIndex] & (1 << bitIndex)) == 0))
                {
                    result = false;
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// Gets if no bits are set in this bit field.
        /// </summary>
        /// <returns>If no bits are set.</returns>
        public bool HasNoneSet()
        {
            bool result = true;
            for (int index = 0; index < length; index++)
            {
                int byteIndex = (index >> 3);
                int bitIndex = (7 - (index & 7));
                if (((buffer[byteIndex] & (1 << bitIndex)) != 0))
                {
                    result = false;
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// Returns the count of set bits in this bit field.
        /// </summary>
        /// <returns>The count of set bits.</returns>
        public int CountSet()
        {
            int count = 0;
            for (int index = 0; index < length; index++)
            {
                int byteIndex = (index >> 3);
                int bitIndex = (7 - (index & 7));
                if (((buffer[byteIndex] & (1 << bitIndex)) != 0))
                {
                    ++count;
                }
            }
            return count;
        }

        /// <summary>
        /// Returns the count of bits we need from another bit field to have as many set bits as possible.
        /// </summary>
        /// <param name="bitField">The other bit field.</param>
        /// <returns>The count of needed bits.</returns>
        public int CountNeeded(BitField bitField)
        {
            if (bitField == null)
                throw new ArgumentNullException("bitField");
            else if (length != bitField.length)
                throw new ArgumentException("The bit field has a different length.", "bitField");

            int count = 0;
            var otherBuffer = bitField.buffer;
            for (int index = 0; index < length; index++)
            {
                int byteIndex = (index >> 3);
                int bitIndex = (7 - (index & 7));
                bool weDontHaveBit = ((buffer[byteIndex] & (1 << bitIndex)) == 0);
                if (weDontHaveBit)
                {
                    bool theyHaveBit = ((otherBuffer[byteIndex] & (1 << bitIndex)) != 0);
                    if (theyHaveBit)
                    {
                        ++count;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// Returns the text-representation of this bit field.
        /// </summary>
        /// <returns>The bit field in text.</returns>
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder(length);
            for (int index = 0; index < length; index++)
            {
                int byteIndex = (index >> 3);
                int bitIndex = (7 - (index & 7));
                if (((buffer[byteIndex] & (1 << bitIndex)) != 0))
                {
                    sb.Append('1');
                }
                else
                {
                    sb.Append('0');
                }
            }
            return sb.ToString();
        }
        #endregion
    }
}
