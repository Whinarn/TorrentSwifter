using System;

namespace TorrentSwifter.Torrents.Modes
{
    /// <summary>
    /// An interface for torrent modes.
    /// </summary>
    public interface ITorrentMode
    {
        /// <summary>
        /// Gets if we should request all peers for the same piece block.
        /// </summary>
        bool RequestAllPeersForSameBlock { get; }
    }
}
