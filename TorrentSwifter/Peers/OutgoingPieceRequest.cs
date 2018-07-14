using System;

namespace TorrentSwifter.Peers
{
    /// <summary>
    /// An outgoing piece request.
    /// </summary>
    internal sealed class OutgoingPieceRequest : IEquatable<OutgoingPieceRequest>
    {
        #region Fields
        private readonly Peer peer;
        private readonly int pieceIndex;
        private readonly int blockIndex;

        private bool isCancelled = false;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the peer this request is to.
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
        /// Gets the index of the block within the piece.
        /// </summary>
        public int BlockIndex
        {
            get { return blockIndex; }
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
        internal OutgoingPieceRequest(Peer peer, int pieceIndex, int blockIndex)
        {
            if (peer == null)
                throw new ArgumentNullException("peer");

            this.peer = peer;
            this.pieceIndex = pieceIndex;
            this.blockIndex = blockIndex;
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
            var request = (obj as OutgoingPieceRequest);
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
        public bool Equals(OutgoingPieceRequest other)
        {
            if (other == null)
                return false;
            else if (peer != other.peer)
                return false;

            return (pieceIndex == other.pieceIndex && blockIndex == other.blockIndex);
        }

        /// <summary>
        /// Returns if this request equals another request.
        /// </summary>
        /// <param name="pieceIndex">The piece index.</param>
        /// <param name="blockIndex">The block index.</param>
        /// <returns>If equals.</returns>
        public bool Equals(int pieceIndex, int blockIndex)
        {
            return (this.pieceIndex == pieceIndex && this.blockIndex == blockIndex);
        }

        /// <summary>
        /// Returns if this request equals another request.
        /// </summary>
        /// <param name="peer">The peer.</param>
        /// <param name="pieceIndex">The piece index.</param>
        /// <param name="blockIndex">The block index.</param>
        /// <returns>If equals.</returns>
        public bool Equals(Peer peer, int pieceIndex, int blockIndex)
        {
            return (this.peer == peer && this.pieceIndex == pieceIndex && this.blockIndex == blockIndex);
        }

        /// <summary>
        /// Returns the hash code for this request.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return peer.GetHashCode() ^ pieceIndex.GetHashCode() << 2 ^ blockIndex.GetHashCode() >> 2;
        }

        /// <summary>
        /// Returns the text-representation of this request.
        /// </summary>
        /// <returns>The text.</returns>
        public override string ToString()
        {
            return string.Format("[Peer:{0}, Piece:{1}, Block:{2}]", peer.ID, pieceIndex, blockIndex);
        }
        #endregion
    }
}
