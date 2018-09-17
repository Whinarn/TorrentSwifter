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

namespace TorrentSwifter.Peers
{
    /// <summary>
    /// Peer connection failed attempt event arguments.
    /// </summary>
    public sealed class PeerConnectionFailedEventArgs : EventArgs
    {
        private readonly ConnectionFailedReason failedReason;

        /// <summary>
        /// Gets the reason of failure for the connection attempt.
        /// </summary>
        public ConnectionFailedReason FailedReason
        {
            get { return failedReason; }
        }

        /// <summary>
        /// Creates new connection failed attempt event arguments.
        /// </summary>
        /// <param name="failedReason">The reason of failure.</param>
        public PeerConnectionFailedEventArgs(ConnectionFailedReason failedReason)
        {
            this.failedReason = failedReason;
        }
    }

    /// <summary>
    /// Peer connection state event arguments.
    /// </summary>
    public sealed class PeerConnectionStateEventArgs : EventArgs
    {
        private readonly bool isInterested;
        private readonly bool isChoked;

        /// <summary>
        /// Gets if the peer is interested.
        /// </summary>
        public bool IsInterested
        {
            get { return isInterested; }
        }

        /// <summary>
        /// Gets if the peer is choking us.
        /// </summary>
        public bool IsChoked
        {
            get { return isChoked; }
        }

        /// <summary>
        /// Creates new connection state event arguments.
        /// </summary>
        /// <param name="isInterested">If the peer is interested.</param>
        /// <param name="isChoked">If the peer is choking us.</param>
        public PeerConnectionStateEventArgs(bool isInterested, bool isChoked)
        {
            this.isInterested = isInterested;
            this.isChoked = isChoked;
        }
    }
}
