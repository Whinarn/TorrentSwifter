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
