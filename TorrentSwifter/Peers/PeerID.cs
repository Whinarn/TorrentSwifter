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

namespace TorrentSwifter.Peers
{
    /// <summary>
    /// A torrent peer ID.
    /// </summary>
    public struct PeerID : IEquatable<PeerID>, IEquatable<byte[]>
    {
        /// <summary>
        /// No peer ID.
        /// </summary>
        public static readonly PeerID None = new PeerID();

        private readonly byte[] id;

        /// <summary>
        /// Gets the hash bytes.
        /// </summary>
        public byte[] ID
        {
            get { return id; }
        }

        /// <summary>
        /// Gets if this peer ID is none.
        /// </summary>
        public bool IsNone
        {
            get { return (id == null); }
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
            if (id == null && other.id == null)
                return true;
            else if (id == null || other.id == null)
                return false;
            else if (id == other.id)
                return true;
            else if (id.Length != other.id.Length)
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
            if (id == null && other == null)
                return true;
            else if (id == null || other == null)
                return false;
            else if (id == other)
                return true;
            else if (id.Length != other.Length)
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
            if (id != null)
            {
                for (int i = 0; i < id.Length; i++)
                {
                    hashCode = ((hashCode * 31) ^ id[i]);
                }
            }
            return hashCode;
        }

        /// <summary>
        /// Returns the url-encoded string for this peer ID.
        /// </summary>
        /// <returns>The url-encoded string.</returns>
        public string ToUrlEncodedString()
        {
            if (id != null)
                return UriHelper.UrlEncodeText(id);
            else
                return string.Empty;
        }

        /// <summary>
        /// Returns the hexadecimal string for this peer ID.
        /// </summary>
        /// <returns>The hexadecimal string.</returns>
        public string ToHexString()
        {
            if (id != null)
                return HexHelper.BytesToHex(id);
            else
                return string.Empty;
        }

        /// <summary>
        /// Returns the text-representation of this peer ID.
        /// </summary>
        /// <returns>The peer client info with ID.</returns>
        public override string ToString()
        {
            if (id != null)
            {
                string idHex = HexHelper.BytesToHex(id);

                string clientName;
                string clientVersion;
                if (PeerHelper.TryGetPeerClientInfo(this, out clientName, out clientVersion))
                {
                    return string.Format("[{0} (v{1}), RAW:{2}]", clientName, clientVersion, idHex);
                }
                else
                {
                    return string.Format("[UNKNOWN, RAW:{0}]", idHex);
                }
            }
            else
            {
                return "<NONE>";
            }
        }
    }
}
