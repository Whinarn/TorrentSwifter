using System;

namespace TorrentSwifter.Torrents.Modes
{
    /// <summary>
    /// An interface for torrent modes.
    /// </summary>
    public interface ITorrentMode
    {
        /// <summary>
        /// Gets or sets the torrent that this mode is assigned with.
        /// </summary>
        Torrent Torrent { get; set; }

        /// <summary>
        /// Gets if we should request all peers for the same piece block.
        /// </summary>
        bool RequestAllPeersForSameBlock { get; }

        /// <summary>
        /// Gets if we mask our bitmasks for connected peers, useful for maquerading as a leecher when for example super-seeding.
        /// Note that this will also disable the have-piece messages normally sent to peers once we have verified a new piece.
        /// </summary>
        bool MaskBitmasks { get; }

        /// <summary>
        /// Updates this mode.
        /// </summary>
        void Update();
    }
}
