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
    /// A torrent info hash.
    /// </summary>
    public struct InfoHash : IEquatable<InfoHash>, IEquatable<byte[]>
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
        /// Creates a new info hash.
        /// </summary>
        /// <param name="hash">The hash bytes.</param>
        public InfoHash(byte[] hash)
        {
            if (hash == null)
                throw new ArgumentNullException("hash");
            else if (hash.Length != 20)
                throw new ArgumentException("The hash must be 20 bytes in size.", "hash");

            this.hash = hash;
        }

        /// <summary>
        /// Creates a new info hash.
        /// </summary>
        /// <param name="hex">The hash as an hexadecimal string.</param>
        public InfoHash(string hex)
        {
            if (hex == null)
                throw new ArgumentNullException("hex");
            else if (hex.Length != 40)
                throw new ArgumentException("The hash must be 20 bytes in size (40 hexadecimal characters).", "hex");

            this.hash = HexHelper.HexToBytes(hex);
        }

        /// <summary>
        /// Returns if this info hash equals another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>If equals.</returns>
        public override bool Equals(object obj)
        {
            if (obj is InfoHash)
                return Equals((InfoHash)obj);
            else
                return false;
        }

        /// <summary>
        /// Returns if this info hash equals another info hash.
        /// </summary>
        /// <param name="other">The other info hash.</param>
        /// <returns>If equals.</returns>
        public bool Equals(InfoHash other)
        {
            if (hash == null && other.hash == null)
                return true;
            else if (hash == null || other.hash == null)
                return false;
            else if (hash == other.hash)
                return true;
            else if (hash.Length != other.hash.Length)
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
        /// Returns if this info hash equals another info hash.
        /// </summary>
        /// <param name="other">The other info hash.</param>
        /// <returns>If equals.</returns>
        public bool Equals(byte[] other)
        {
            if (hash == null && other == null)
                return true;
            else if (hash == null || other == null)
                return false;
            else if (hash == other)
                return true;
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
        /// Returns the hash code of this info hash.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            int hashCode = 17;
            if (hash != null)
            {
                for (int i = 0; i < hash.Length; i++)
                {
                    hashCode = ((hashCode * 31) ^ hash[i]);
                }
            }
            return hashCode;
        }

        /// <summary>
        /// Returns the url-encoded string for this info hash.
        /// </summary>
        /// <returns>The url-encoded string.</returns>
        public string ToUrlEncodedString()
        {
            if (hash != null)
                return UriHelper.UrlEncodeText(hash);
            else
                return string.Empty;
        }

        /// <summary>
        /// Returns the hexadecimal string for this info hash.
        /// </summary>
        /// <returns>The hash in hexadecimals.</returns>
        public string ToHexString()
        {
            if (hash != null)
                return HexHelper.BytesToHex(hash);
            else
                return string.Empty;
        }

        /// <summary>
        /// Returns the text-representation of this info hash.
        /// </summary>
        /// <returns>The hash string.</returns>
        public override string ToString()
        {
            if (hash != null)
                return HexHelper.BytesToHex(hash);
            else
                return "<NONE>";
        }
    }
}
