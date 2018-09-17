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
using System.Net;
using System.Threading.Tasks;
using TorrentSwifter.Torrents;

namespace TorrentSwifter.Peers
{
    /// <summary>
    /// A torrent peer connection.
    /// </summary>
    public abstract class PeerConnection : IDisposable
    {
        #region Fields
        /// <summary>
        /// The parent torrent.
        /// </summary>
        protected Torrent torrent = null;
        /// <summary>
        /// The parent peer.
        /// </summary>
        protected Peer peer = null;
        /// <summary>
        /// The peer end-point.
        /// </summary>
        protected readonly EndPoint endPoint;

        private BitField remoteBitField = null;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the parent torrent for this peer connection.
        /// </summary>
        public Torrent Torrent
        {
            get { return torrent; }
        }

        /// <summary>
        /// Gets the parent peer for this peer connection.
        /// </summary>
        public Peer Peer
        {
            get { return peer; }
        }

        /// <summary>
        /// Gets the peer end-point.
        /// </summary>
        public EndPoint EndPoint
        {
            get { return endPoint; }
        }

        /// <summary>
        /// Gets if we are currently attempting to connect.
        /// </summary>
        public abstract bool IsConnecting
        {
            get;
        }

        /// <summary>
        /// Gets if this connection has been established.
        /// </summary>
        public abstract bool IsConnected
        {
            get;
        }

        /// <summary>
        /// Gets if this connection has handshaked successfully.
        /// </summary>
        public abstract bool IsHandshaked
        {
            get;
        }

        /// <summary>
        /// Gets if we are currently choking the peer.
        /// </summary>
        public abstract bool IsChokedByUs
        {
            get;
        }

        /// <summary>
        /// Gets if we are interested in this peer.
        /// </summary>
        public abstract bool IsInterestedByUs
        {
            get;
        }

        /// <summary>
        /// Gets if we are currently choked by the remote.
        /// </summary>
        public abstract bool IsChokedByRemote
        {
            get;
        }

        /// <summary>
        /// Gets if this peer is interested in some of our pieces.
        /// </summary>
        public abstract bool IsInterestedByRemote
        {
            get;
        }

        /// <summary>
        /// Gets the ID of the peer we are connected to.
        /// </summary>
        public abstract PeerID PeerID
        {
            get;
        }

        /// <summary>
        /// Gets the full bit field for the remote.
        /// Note that this can be null.
        /// </summary>
        public BitField RemoteBitField
        {
            get { return remoteBitField; }
        }
        #endregion

        #region Events
        /// <summary>
        /// Occurs when a connection has been established with this peer.
        /// </summary>
        public event EventHandler Connected;
        /// <summary>
        /// Occurs when a connection attempt has failed with this peer.
        /// </summary>
        public event EventHandler<PeerConnectionFailedEventArgs> ConnectionFailed;
        /// <summary>
        /// Occurs when our connection with this peer has been disconnected.
        /// </summary>
        public event EventHandler Disconnected;
        /// <summary>
        /// Occurs when we have handshaked with this peer.
        /// </summary>
        public event EventHandler Handshaked;
        /// <summary>
        /// Occurs when the state of this peer has changed.
        /// </summary>
        public event EventHandler<PeerConnectionStateEventArgs> StateChanged;
        /// <summary>
        /// Occurs when the full bit field has been received.
        /// </summary>
        public event EventHandler<BitFieldEventArgs> BitFieldReceived;
        /// <summary>
        /// Occurs when the peer has reported having a new piece.
        /// </summary>
        public event EventHandler<PieceEventArgs> HavePieceReceived;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new torrent peer connection.
        /// </summary>
        /// <param name="endPoint">The peer end-point.</param>
        public PeerConnection(EndPoint endPoint)
        {
            if (endPoint == null)
                throw new ArgumentNullException("endPoint");

            this.torrent = null;
            this.peer = null;
            this.endPoint = endPoint;
        }

        /// <summary>
        /// Creates a new torrent peer connection.
        /// </summary>
        /// <param name="torrent">The parent torrent.</param>
        /// <param name="peer">The parent peer.</param>
        /// <param name="endPoint">The peer end-point.</param>
        public PeerConnection(Torrent torrent, Peer peer, EndPoint endPoint)
        {
            if (torrent == null)
                throw new ArgumentNullException("torrent");
            else if (peer == null)
                throw new ArgumentNullException("torrent");
            else if (endPoint == null)
                throw new ArgumentNullException("endPoint");

            this.torrent = torrent;
            this.peer = peer;
            this.endPoint = endPoint;
        }
        #endregion

        #region Finalizer
        /// <summary>
        /// The finalizer.
        /// </summary>
        ~PeerConnection()
        {
            Dispose(false);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Disposes of this connection.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        /// <summary>
        /// Connects to this peer synchronously.
        /// </summary>
        public abstract void Connect();

        /// <summary>
        /// Connects to this peer asynchronously.
        /// </summary>
        /// <returns>The connect task.</returns>
        public abstract Task ConnectAsync();

        /// <summary>
        /// Disconnects from this peer.
        /// </summary>
        public abstract void Disconnect();

        /// <summary>
        /// Updates this peer connection.
        /// </summary>
        public abstract void Update();

        /// <summary>
        /// Sends the interest state to this peer.
        /// </summary>
        /// <param name="state">If interested (true), or not (false).</param>
        public abstract void SendInterested(bool state);

        /// <summary>
        /// Sends the choked state to this peer.
        /// </summary>
        /// <param name="state">If choked (true), or not (false).</param>
        public abstract void SendChoked(bool state);

        /// <summary>
        /// Reports that we have a new piece to this peer.
        /// </summary>
        /// <param name="pieceIndex">The piece index.</param>
        public abstract void ReportHavePiece(int pieceIndex);

        /// <summary>
        /// Requests a piece of data from this peer.
        /// </summary>
        /// <param name="pieceIndex">The piece index.</param>
        /// <param name="blockIndex">The block index.</param>
        /// <returns>The request task, with a result if the request was sent.</returns>
        public abstract Task<bool> RequestPieceData(int pieceIndex, int blockIndex);

        /// <summary>
        /// Cancels a pending request for a piece of data from this peer.
        /// </summary>
        /// <param name="pieceIndex">The piece index.</param>
        /// <param name="blockIndex">The block index.</param>
        public abstract void CancelPieceDataRequest(int pieceIndex, int blockIndex);

        /// <summary>
        /// Sends a piece of data to this peer.
        /// </summary>
        /// <param name="pieceIndex">The piece index.</param>
        /// <param name="begin">The byte offset within the piece.</param>
        /// <param name="data">The byte data.</param>
        /// <returns>The send task.</returns>
        public abstract Task SendPieceData(int pieceIndex, int begin, byte[] data);
        #endregion

        #region Protected Methods
        /// <summary>
        /// Disposes of this peer connection.
        /// </summary>
        /// <param name="disposing">If disposing, otherwise finalizing.</param>
        protected abstract void Dispose(bool disposing);

        /// <summary>
        /// The peer connection has been successfully established.
        /// </summary>
        protected virtual void OnConnected()
        {
            Connected.SafeInvoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// The connection attempt to this peer has failed.
        /// </summary>
        /// <param name="failedReason">The reason of failure.</param>
        protected virtual void OnConnectionFailed(ConnectionFailedReason failedReason)
        {
            var eventArgs = new PeerConnectionFailedEventArgs(failedReason);
            ConnectionFailed.SafeInvoke(this, eventArgs);
        }

        /// <summary>
        /// The peer connection has been disconnected.
        /// </summary>
        protected virtual void OnDisconnected()
        {
            Disconnected.SafeInvoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// The peer connection has been successfully handshaked.
        /// </summary>
        protected virtual void OnHandshaked()
        {
            Handshaked.SafeInvoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// The state of the peer has been changed.
        /// </summary>
        protected virtual void OnStateChanged()
        {
            var eventArgs = new PeerConnectionStateEventArgs(IsInterestedByRemote, IsChokedByRemote);
            StateChanged.SafeInvoke(this, eventArgs);
        }

        /// <summary>
        /// The full bit field has been received from the peer.
        /// </summary>
        /// <param name="bitField">The bit field.</param>
        protected virtual void OnBitFieldReceived(BitField bitField)
        {
            if (remoteBitField != null)
            {
                bitField.CopyTo(remoteBitField);
            }
            else
            {
                remoteBitField = bitField;
            }

            BitFieldReceived.SafeInvoke(this, new BitFieldEventArgs(bitField));
        }

        /// <summary>
        /// The peer has reported having an additional piece.
        /// </summary>
        /// <param name="pieceIndex">The piece index.</param>
        protected virtual void OnHavePiece(int pieceIndex)
        {
            if (remoteBitField == null)
            {
                remoteBitField = new BitField(torrent.PieceCount);
            }

            remoteBitField.Set(pieceIndex, true);

            HavePieceReceived.SafeInvoke(this, new PieceEventArgs(pieceIndex));
        }
        #endregion
    }
}
