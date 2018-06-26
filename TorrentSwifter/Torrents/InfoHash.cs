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
        /// Returns if this info hash equals another info hash.
        /// </summary>
        /// <param name="other">The other info hash.</param>
        /// <returns>If equals.</returns>
        public bool Equals(byte[] other)
        {
            if (hash.Length != other.Length)
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
            for (int i = 0; i < hash.Length; i++)
            {
                hashCode = ((hashCode * 31) ^ hash[i]);
            }
            return hashCode;
        }

        /// <summary>
        /// Returns the url-encoded string for this info hash.
        /// </summary>
        /// <returns>The url-encoded string.</returns>
        public string ToUrlEncodedString()
        {
            return UriHelper.UrlEncodeText(hash);
        }

        /// <summary>
        /// Returns the text-representation of this info hash.
        /// </summary>
        /// <returns>The hash in hexadecimals.</returns>
        public override string ToString()
        {
            return HexHelper.BytesToHex(hash);
        }
    }
}
