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
using TorrentSwifter.Helpers;

namespace TorrentSwifter.Torrents
{
    /// <summary>
    /// A piece hash.
    /// </summary>
    public struct PieceHash : IEquatable<PieceHash>, IEquatable<byte[]>
    {
        private readonly byte[] hash;

        /// <summary>
        /// Gets the hash bytes.
        /// </summary>
        public byte[] Hash
        {
            get { return hash; }
        }

        /// <summary>
        /// Creates a new piece hash.
        /// </summary>
        /// <param name="hash">The hash bytes.</param>
        public PieceHash(byte[] hash)
        {
            if (hash == null)
                throw new ArgumentNullException("hash");
            else if (hash.Length != 20)
                throw new ArgumentException("The hash must be 20 bytes in size.", "hash");

            this.hash = hash;
        }

        /// <summary>
        /// Returns if this piece hash equals another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>If equals.</returns>
        public override bool Equals(object obj)
        {
            if (obj is PieceHash)
                return Equals((PieceHash)obj);
            else
                return false;
        }

        /// <summary>
        /// Returns if this piece hash equals another piece hash.
        /// </summary>
        /// <param name="other">The other piece hash.</param>
        /// <returns>If equals.</returns>
        public bool Equals(PieceHash other)
        {
            if (hash.Length != other.hash.Length)
                return false;

            bool result = true;
            for (int i = 0; i < hash.Length; i++)
            {
                if (hash[i] != other.hash[i])
                {
                    result = false;
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// Returns if this piece hash equals another piece hash.
        /// </summary>
        /// <param name="other">The other piece hash.</param>
        /// <returns>If equals.</returns>
        public bool Equals(byte[] other)
        {
            if (other == null)
                return false;
            else if (hash.Length != other.Length)
                return false;

            bool result = true;
            for (int i = 0; i < hash.Length; i++)
            {
                if (hash[i] != other[i])
                {
                    result = false;
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// Returns the hash code of this piece hash.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            int hashCode = 17;
            for (int i = 0; i < hash.Length; i++)
            {
                hashCode = ((hashCode * 31) ^ hash[i]);
            }
            return hashCode;
        }

        /// <summary>
        /// Returns the text-representation of this piece hash.
        /// </summary>
        /// <returns>The hash in hexadecimals.</returns>
        public override string ToString()
        {
            return HexHelper.BytesToHex(hash);
        }
    }
}
