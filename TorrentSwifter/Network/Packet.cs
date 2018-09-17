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
using System.IO;
using System.Text;

namespace TorrentSwifter.Network
{
    /// <summary>
    /// A data packet.
    /// </summary>
    public class Packet
    {
        #region Fields
        private byte[] data = null;
        private int offset = 0;
        private int length = 0;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the full data buffer for this packet.
        /// </summary>
        public byte[] Data
        {
            get { return data; }
        }

        /// <summary>
        /// Gets or sets the read/write offset within the packet data buffer.
        /// </summary>
        public int Offset
        {
            get { return offset; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value");

                EnsureCapacity(value);
                offset = value;
            }
        }

        /// <summary>
        /// Gets or sets the length of the packet.
        /// </summary>
        public int Length
        {
            get { return length; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value");

                EnsureCapacity(value);
                length = value;
            }
        }

        /// <summary>
        /// Gets the count of remaining bytes to read from this packet.
        /// </summary>
        public int RemainingBytes
        {
            get { return (length - offset); }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new packet with an initial capacity.
        /// </summary>
        /// <param name="capacity">The initial capacity.</param>
        public Packet(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException("capacity");

            this.data = new byte[capacity];
            this.offset = 0;
            this.length = 0;
        }

        /// <summary>
        /// Creates a new packet with existing data.
        /// </summary>
        /// <param name="data">The existing data-buffer.</param>
        /// <param name="length">The length of the packet within the data-buffer.</param>
        public Packet(byte[] data, int length)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            else if (length < 0 || length > data.Length)
                throw new ArgumentOutOfRangeException("length");

            this.data = data;
            this.offset = 0;
            this.length = length;
        }
        #endregion

        #region Public Methods
        #region Reading
        /// <summary>
        /// Reads a byte from the packet.
        /// </summary>
        public byte ReadByte()
        {
            if (offset >= length)
                throw new EndOfStreamException();

            return data[offset++];
        }

        /// <summary>
        /// Reads a signed 16-bit integer from the packet.
        /// </summary>
        public short ReadInt16()
        {
            if ((offset + 1) >= length)
                throw new EndOfStreamException();

            short value = (short)((data[offset] << 8) | data[offset + 1]);
            offset += 2;
            return value;
        }

        /// <summary>
        /// Reads an unsigned 16-bit integer from the packet.
        /// </summary>
        public ushort ReadUInt16()
        {
            if ((offset + 1) >= length)
                throw new EndOfStreamException();

            ushort value = (ushort)((data[offset] << 8) | data[offset + 1]);
            offset += 2;
            return value;
        }

        /// <summary>
        /// Reads a signed 32-bit integer from the packet.
        /// </summary>
        public int ReadInt32()
        {
            if ((offset + 3) >= length)
                throw new EndOfStreamException();

            int value = ((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
            offset += 4;
            return value;
        }

        /// <summary>
        /// Reads an unsigned 32-bit integer from the packet.
        /// </summary>
        public uint ReadUInt32()
        {
            if ((offset + 3) >= length)
                throw new EndOfStreamException();

            uint value = (((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | (uint)data[offset + 3]);
            offset += 4;
            return value;
        }

        /// <summary>
        /// Reads a signed 64-bit integer from the packet.
        /// </summary>
        public long ReadInt64()
        {
            if ((offset + 7) >= length)
                throw new EndOfStreamException();

            long value = (((long)data[offset] << 56) | ((long)data[offset + 1] << 48) | ((long)data[offset + 2] << 40) | ((long)data[offset + 3] << 32) |
                          ((long)data[offset + 4] << 24) | ((long)data[offset + 5] << 16) | ((long)data[offset + 6] << 8) | (long)data[offset + 7]);
            offset += 8;
            return value;
        }

        /// <summary>
        /// Reads an unsigned 64-bit integer from the packet.
        /// </summary>
        public ulong ReadUInt64()
        {
            if ((offset + 7) >= length)
                throw new EndOfStreamException();

            ulong value = (((ulong)data[offset] << 56) | ((ulong)data[offset + 1] << 48) | ((ulong)data[offset + 2] << 40) | ((ulong)data[offset + 3] << 32) |
                          ((ulong)data[offset + 4] << 24) | ((ulong)data[offset + 5] << 16) | ((ulong)data[offset + 6] << 8) | (ulong)data[offset + 7]);
            offset += 8;
            return value;
        }

        /// <summary>
        /// Reads a single-precision floating-point number from the packet.
        /// </summary>
        public unsafe float ReadFloat()
        {
            if ((offset + 3) >= length)
                throw new EndOfStreamException();

            int intValue = ((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
            offset += 4;
            return *(float*)&intValue;
        }

        /// <summary>
        /// Reads a single-precision floating-point number from the packet.
        /// </summary>
        public unsafe double ReadDouble()
        {
            if ((offset + 7) >= length)
                throw new EndOfStreamException();

            long longValue = (((long)data[offset] << 56) | ((long)data[offset + 1] << 48) | ((long)data[offset + 2] << 40) | ((long)data[offset + 3] << 32) |
                          ((long)data[offset + 4] << 24) | ((long)data[offset + 5] << 16) | ((long)data[offset + 6] << 8) | (long)data[offset + 7]);
            offset += 8;
            return *(double*)&longValue;
        }

        /// <summary>
        /// Reads into a byte-buffer from the packet.
        /// </summary>
        /// <param name="buffer">The byte-buffer to read into.</param>
        /// <param name="offset">The offset within the byte-buffer to start writing to.</param>
        /// <param name="count">The count of bytes to read.</param>
        /// <returns>The count of bytes actually read.</returns>
        public int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            else if (offset < 0 || offset >= buffer.Length)
                throw new ArgumentOutOfRangeException("offset");
            else if (count < 0 || (offset + count) > buffer.Length)
                throw new ArgumentOutOfRangeException("count");
            else if (count == 0)
                return 0;

            if ((offset + count) > length)
            {
                count = length - offset;
                if (count <= 0)
                    return 0;
            }

            Buffer.BlockCopy(this.data, this.offset, buffer, offset, count);
            this.offset += count;
            return count;
        }

        /// <summary>
        /// Reads a count of bytes from the packet into a new buffer.
        /// </summary>
        /// <param name="count">The count of bytes to read.</param>
        /// <returns>The read byte-buffer.</returns>
        public byte[] ReadBytes(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
            else if ((offset + count) > length)
                throw new EndOfStreamException();
            else if (count == 0)
                return new byte[0];

            var buffer = new byte[count];
            Buffer.BlockCopy(data, offset, buffer, 0, count);
            offset += count;
            return buffer;
        }

        /// <summary>
        /// Reads an ASCII text string from the packet.
        /// </summary>
        /// <param name="length">The length of the text string to read in bytes.</param>
        /// <returns>The read string.</returns>
        public string ReadString(int length)
        {
            return ReadString(length, Encoding.ASCII);
        }

        /// <summary>
        /// Reads a text string from the packet.
        /// </summary>
        /// <param name="length">The length of the text string to read in bytes.</param>
        /// <param name="encoding">The text encoding to use.</param>
        /// <returns>The read string.</returns>
        public string ReadString(int length, Encoding encoding)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException("length");
            else if (encoding == null)
                throw new ArgumentNullException("encoding");
            else if (length == 0)
                return string.Empty;

            byte[] stringBytes = new byte[length];
            int readByteCount = Read(stringBytes, 0, length);
            if (readByteCount == 0)
                return string.Empty;

            return encoding.GetString(stringBytes, 0, readByteCount);
        }
        #endregion

        #region Writing
        /// <summary>
        /// Writes a byte to the packet.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteByte(byte value)
        {
            EnsureCapacity(offset + 1);

            data[offset++] = value;

            if (offset > length)
                length = offset;
        }

        /// <summary>
        /// Writes a signed 16-bit integer to the packet.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteInt16(short value)
        {
            EnsureCapacity(offset + 2);

            data[offset + 0] = (byte)((value >> 8) & 0xFF);
            data[offset + 1] = (byte)((value) & 0xFF);
            offset += 2;

            if (offset > length)
                length = offset;
        }

        /// <summary>
        /// Writes an unsigned 16-bit integer to the packet.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteUInt16(ushort value)
        {
            EnsureCapacity(offset + 2);

            data[offset + 0] = (byte)((value >> 8) & 0xFF);
            data[offset + 1] = (byte)((value) & 0xFF);
            offset += 2;

            if (offset > length)
                length = offset;
        }

        /// <summary>
        /// Writes a signed 32-bit integer to the packet.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteInt32(int value)
        {
            EnsureCapacity(offset + 4);

            data[offset + 0] = (byte)((value >> 24) & 0xFF);
            data[offset + 1] = (byte)((value >> 16) & 0xFF);
            data[offset + 2] = (byte)((value >> 8) & 0xFF);
            data[offset + 3] = (byte)((value) & 0xFF);
            offset += 4;

            if (offset > length)
                length = offset;
        }

        /// <summary>
        /// Writes an unsigned 32-bit integer to the packet.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteUInt32(uint value)
        {
            EnsureCapacity(offset + 4);

            data[offset + 0] = (byte)((value >> 24) & 0xFF);
            data[offset + 1] = (byte)((value >> 16) & 0xFF);
            data[offset + 2] = (byte)((value >> 8) & 0xFF);
            data[offset + 3] = (byte)((value) & 0xFF);
            offset += 4;

            if (offset > length)
                length = offset;
        }

        /// <summary>
        /// Writes a signed 64-bit integer to the packet.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteInt64(long value)
        {
            EnsureCapacity(offset + 8);

            data[offset + 0] = (byte)((value >> 56) & 0xFF);
            data[offset + 1] = (byte)((value >> 48) & 0xFF);
            data[offset + 2] = (byte)((value >> 40) & 0xFF);
            data[offset + 3] = (byte)((value >> 32) & 0xFF);
            data[offset + 4] = (byte)((value >> 24) & 0xFF);
            data[offset + 5] = (byte)((value >> 16) & 0xFF);
            data[offset + 6] = (byte)((value >> 8) & 0xFF);
            data[offset + 7] = (byte)((value) & 0xFF);
            offset += 8;

            if (offset > length)
                length = offset;
        }

        /// <summary>
        /// Writes an unsigned 64-bit integer to the packet.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteUInt64(ulong value)
        {
            EnsureCapacity(offset + 8);

            data[offset + 0] = (byte)((value >> 56) & 0xFF);
            data[offset + 1] = (byte)((value >> 48) & 0xFF);
            data[offset + 2] = (byte)((value >> 40) & 0xFF);
            data[offset + 3] = (byte)((value >> 32) & 0xFF);
            data[offset + 4] = (byte)((value >> 24) & 0xFF);
            data[offset + 5] = (byte)((value >> 16) & 0xFF);
            data[offset + 6] = (byte)((value >> 8) & 0xFF);
            data[offset + 7] = (byte)((value) & 0xFF);
            offset += 8;

            if (offset > length)
                length = offset;
        }

        /// <summary>
        /// Writes a single-precision floating-point number to the packet.
        /// </summary>
        /// <param name="value">The value.</param>
        public unsafe void WriteFloat(float value)
        {
            EnsureCapacity(offset + 4);

            int intValue = *(int*)&value;
            data[offset + 0] = (byte)((intValue >> 24) & 0xFF);
            data[offset + 1] = (byte)((intValue >> 16) & 0xFF);
            data[offset + 2] = (byte)((intValue >> 8) & 0xFF);
            data[offset + 3] = (byte)((intValue) & 0xFF);
            offset += 4;

            if (offset > length)
                length = offset;
        }

        /// <summary>
        /// Writes a double-precision floating-point number to the packet.
        /// </summary>
        /// <param name="value">The value.</param>
        public unsafe void WriteDouble(double value)
        {
            EnsureCapacity(offset + 8);

            long longValue = *(long*)&value;
            data[offset + 0] = (byte)((longValue >> 56) & 0xFF);
            data[offset + 1] = (byte)((longValue >> 48) & 0xFF);
            data[offset + 2] = (byte)((longValue >> 40) & 0xFF);
            data[offset + 3] = (byte)((longValue >> 32) & 0xFF);
            data[offset + 4] = (byte)((longValue >> 24) & 0xFF);
            data[offset + 5] = (byte)((longValue >> 16) & 0xFF);
            data[offset + 6] = (byte)((longValue >> 8) & 0xFF);
            data[offset + 7] = (byte)((longValue) & 0xFF);
            offset += 8;

            if (offset > length)
                length = offset;
        }

        /// <summary>
        /// Writes a byte-buffer to the packet.
        /// </summary>
        /// <param name="buffer">The byte-buffer to write.</param>
        /// <param name="offset">The offset within the byte-buffer.</param>
        /// <param name="count">The count of bytes to write.</param>
        public void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            else if (offset < 0 || offset >= buffer.Length)
                throw new ArgumentOutOfRangeException("offset");
            else if (count < 0 || (offset + count) > buffer.Length)
                throw new ArgumentOutOfRangeException("count");
            else if (count == 0)
                return;

            EnsureCapacity(this.offset + count);
            Buffer.BlockCopy(buffer, offset, this.data, this.offset, count);
            this.offset += count;

            if (this.offset > this.length)
                this.length = this.offset;
        }

        /// <summary>
        /// Writes an ASCII text string to the packet.
        /// </summary>
        /// <param name="text">The text string.</param>
        public void WriteString(string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");

            WriteString(text, Encoding.ASCII);
        }

        /// <summary>
        /// Writes a text string to the packet.
        /// </summary>
        /// <param name="text">The text string.</param>
        /// <param name="encoding">The text encoding to use.</param>
        public void WriteString(string text, Encoding encoding)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            else if (encoding == null)
                throw new ArgumentNullException("encoding");

            byte[] stringBytes = encoding.GetBytes(text);
            Write(stringBytes, 0, stringBytes.Length);
        }

        /// <summary>
        /// Writes a specific count of zero bytes to the packet.
        /// </summary>
        /// <param name="count">The count of bytes.</param>
        public void WriteZeroes(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
            else if (count == 0)
                return;

            EnsureCapacity(this.offset + count);
            this.offset += count;

            if (this.offset > this.length)
                this.length = this.offset;
        }
        #endregion

        #region Skipping
        /// <summary>
        /// Skip a specific count of bytes.
        /// </summary>
        /// <param name="count">The count of bytes to skip.</param>
        public void Skip(int count)
        {
            if ((offset + count) > length)
                throw new EndOfStreamException();

            offset += count;
        }
        #endregion
        #endregion

        #region Private Methods
        private void EnsureCapacity(int neededCapacity)
        {
            if (data.Length < neededCapacity)
            {
                int newCapacity = Math.Max(data.Length << 1, neededCapacity + 28);
                var newData = new byte[newCapacity];
                Buffer.BlockCopy(data, 0, newData, 0, length);
                data = newData;
            }
        }
        #endregion
    }
}
