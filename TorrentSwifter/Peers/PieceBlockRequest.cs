﻿using System;

namespace TorrentSwifter.Peers
{
    /// <summary>
    /// A piece block request.
    /// </summary>
    internal sealed class PieceBlockRequest : IEquatable<PieceBlockRequest>
    {
        #region Fields
        private readonly Peer peer;
        private readonly int pieceIndex;
        private readonly int begin;
        private readonly int length;

        private bool isCancelled = false;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the peer this request is from or to.
        /// </summary>
        public Peer Peer
        {
            get { return peer; }
        }

        /// <summary>
        /// Gets the index of the piece this request is for.
        /// </summary>
        public int PieceIndex
        {
            get { return pieceIndex; }
        }

        /// <summary>
        /// Gets the byte offset within the piece.
        /// </summary>
        public int Begin
        {
            get { return begin; }
        }

        /// <summary>
        /// Gets the length in bytes requested for the piece.
        /// </summary>
        public int Length
        {
            get { return length; }
        }

        /// <summary>
        /// Gets or sets if this request has been cancelled.
        /// </summary>
        public bool IsCancelled
        {
            get { return isCancelled; }
            set { isCancelled = value; }
        }
        #endregion

        #region Constructor
        internal PieceBlockRequest(Peer peer, int pieceIndex, int begin, int length)
        {
            if (peer == null)
                throw new ArgumentNullException("peer");

            this.peer = peer;
            this.pieceIndex = pieceIndex;
            this.begin = begin;
            this.length = length;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Returns if this request equals another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>If equals.</returns>
        public override bool Equals(object obj)
        {
            var request = (obj as PieceBlockRequest);
            if (request != null)
                return Equals(request);
            else
                return false;
        }

        /// <summary>
        /// Returns if this request equals another request.
        /// </summary>
        /// <param name="other">The other request.</param>
        /// <returns>If equals.</returns>
        public bool Equals(PieceBlockRequest other)
        {
            if (other == null)
                return false;
            else if (peer != other.peer)
                return false;

            return (pieceIndex == other.pieceIndex && begin == other.begin && length == other.length);
        }

        /// <summary>
        /// Returns if this request equals another request.
        /// </summary>
        /// <param name="pieceIndex">The piece index.</param>
        /// <param name="begin">The byte offset within the piece.</param>
        /// <param name="length">The length in bytes.</param>
        /// <returns>If equals.</returns>
        public bool Equals(int pieceIndex, int begin, int length)
        {
            return (this.pieceIndex == pieceIndex && this.begin == begin && this.length == length);
        }

        /// <summary>
        /// Returns if this request equals another request.
        /// </summary>
        /// <param name="peer">The peer.</param>
        /// <param name="pieceIndex">The piece index.</param>
        /// <param name="begin">The byte offset within the piece.</param>
        /// <param name="length">The length in bytes.</param>
        /// <returns>If equals.</returns>
        public bool Equals(Peer peer, int pieceIndex, int begin, int length)
        {
            return (this.peer == peer && this.pieceIndex == pieceIndex && this.begin == begin && this.length == length);
        }

        /// <summary>
        /// Returns the hash code for this request.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return peer.GetHashCode() ^ pieceIndex.GetHashCode() << 2 ^ begin.GetHashCode() >> 2 ^ length.GetHashCode() >> 1;
        }

        /// <summary>
        /// Returns the text-representation of this request.
        /// </summary>
        /// <returns>The text.</returns>
        public override string ToString()
        {
            return string.Format("[Peer:{0}, Piece:{1}, Begin:{2}, Length:{3}]", peer.ID, pieceIndex, begin, length);
        }
        #endregion
    }
}
