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
using TorrentSwifter.Torrents;

namespace TorrentSwifter.Peers
{
    /// <summary>
    /// An outgoing piece request.
    /// </summary>
    internal sealed class OutgoingPieceRequest : IEquatable<OutgoingPieceRequest>
    {
        #region Fields
        private readonly Torrent torrent;
        private readonly Peer peer;
        private readonly int pieceIndex;
        private readonly int blockIndex;

        private DateTime requestTime;
        private bool hasBeenSent = false;
        private bool isCancelled = false;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the torrent this request was for.
        /// </summary>
        public Torrent Torrent
        {
            get { return torrent; }
        }

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
        /// Gets the time when this request was sent.
        /// </summary>
        public DateTime RequestTime
        {
            get { return requestTime; }
        }

        /// <summary>
        /// Gets the age of the request (from when it was sent) in milliseconds.
        /// </summary>
        public int RequestAge
        {
            get
            {
                var dateNow = DateTime.UtcNow;
                if (requestTime > dateNow)
                    return 0;

                return (int)DateTime.UtcNow.Subtract(requestTime).TotalMilliseconds;
            }
        }

        /// <summary>
        /// Gets if this request has been cancelled.
        /// </summary>
        public bool IsCancelled
        {
            get { return isCancelled; }
        }
        #endregion

        #region Constructor
        internal OutgoingPieceRequest(Torrent torrent, Peer peer, int pieceIndex, int blockIndex)
        {
            if (torrent == null)
                throw new ArgumentNullException("torrent");
            else if (peer == null)
                throw new ArgumentNullException("peer");

            this.torrent = torrent;
            this.peer = peer;
            this.pieceIndex = pieceIndex;
            this.blockIndex = blockIndex;
            this.requestTime = DateTime.UtcNow;
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

        #region Internal Methods
        internal void OnSent()
        {
            requestTime = DateTime.UtcNow;
            hasBeenSent = true;
        }

        internal void OnCancelSent()
        {
            hasBeenSent = false;
        }

        internal void Cancel()
        {
            if (isCancelled)
                return;

            isCancelled = true;

            if (hasBeenSent)
            {
                Logging.Log.LogDebug("[Peer][{0}] We are cancelling a piece request. Index: {1}, Block: {2}", peer.EndPoint, pieceIndex, blockIndex);

                // Send the cancel message to the peer because we have already sent this request to them
                if (peer.IsConnected)
                {
                    peer.CancelPieceDataRequest(pieceIndex, blockIndex);
                }

                // Remove the peer from the request list from the requested block
                var piece = torrent.GetPiece(pieceIndex);
                var block = piece.GetBlock(blockIndex);
                block.RemoveRequestPeer(peer);
            }

            peer.UnregisterPieceRequest(this);
        }
        #endregion
    }
}
