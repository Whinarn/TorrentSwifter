using System;
using TorrentSwifter.Helpers;

namespace TorrentSwifter.Peers
{
    /// <summary>
    /// A torrent peer ID.
    /// </summary>
    public struct PeerID : IEquatable<PeerID>, IEquatable<byte[]>
    {
        private readonly byte[] id;

        /// <summary>
        /// Gets the hash bytes.
        /// </summary>
        public byte[] ID
        {
            get { return id; }
        }

        /// <summary>
        /// Creates a new peer ID.
        /// </summary>
        /// <param name="id">The ID bytes.</param>
        public PeerID(byte[] id)
        {
            if (id == null)
                throw new ArgumentNullException("id");
            else if (id.Length != 20)
                throw new ArgumentException("The peer ID must be 20 bytes in size.", "id");

            this.id = id;
        }

        /// <summary>
        /// Returns if this peer ID equals another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>If equals.</returns>
        public override bool Equals(object obj)
        {
            if (obj is PeerID)
                return Equals((PeerID)obj);
            else
                return false;
        }

        /// <summary>
        /// Returns if this peer ID equals another peer ID.
        /// </summary>
        /// <param name="other">The other peer ID.</param>
        /// <returns>If equals.</returns>
        public bool Equals(PeerID other)
        {
            if (id.Length != other.id.Length)
                return false;

            bool result = true;
            for (int i = 0; i < id.Length; i++)
            {
                if (id[i] != other.id[i])
                {
                    result = false;
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// Returns if this peer ID equals another peer ID.
        /// </summary>
        /// <param name="other">The other peer ID.</param>
        /// <returns>If equals.</returns>
        public bool Equals(byte[] other)
        {
            if (id.Length != other.Length)
                return false;

            bool result = true;
            for (int i = 0; i < id.Length; i++)
            {
                if (id[i] != other[i])
                {
                    result = false;
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// Returns the hash code of this peer ID.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            int hashCode = 17;
            for (int i = 0; i < id.Length; i++)
            {
                hashCode = ((hashCode * 31) ^ id[i]);
            }
            return hashCode;
        }

        /// <summary>
        /// Returns the url-encoded string for this info hash.
        /// </summary>
        /// <returns>The url-encoded string.</returns>
        public string ToUrlEncodedString()
        {
            return UriHelper.UrlEncodeText(id);
        }

        /// <summary>
        /// Returns the hexadecimal string for this info hash.
        /// </summary>
        /// <returns>The hexadecimal string.</returns>
        public string ToHexString()
        {
            return HexHelper.BytesToHex(id);
        }

        /// <summary>
        /// Returns the text-representation of this peer ID.
        /// </summary>
        /// <returns>The peer ID in hexadecimals.</returns>
        public override string ToString()
        {
            // TODO: Format with client name, version, etc..
            return HexHelper.BytesToHex(id);
        }
    }
}
