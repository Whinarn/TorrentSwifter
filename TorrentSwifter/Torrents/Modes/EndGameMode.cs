using System;

namespace TorrentSwifter.Torrents.Modes
{
    /// <summary>
    /// End-game downloading mode.
    /// </summary>
    public class EndGameMode : ITorrentMode
    {
        /// <summary>
        /// Gets if we should request all peers for the same piece block.
        /// </summary>
        public bool RequestAllPeersForSameBlock
        {
            get { return true; }
        }
    }
}
