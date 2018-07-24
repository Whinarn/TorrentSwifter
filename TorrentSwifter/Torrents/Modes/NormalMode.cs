using System;

namespace TorrentSwifter.Torrents.Modes
{
    /// <summary>
    /// Normal downloading and uploading mode.
    /// </summary>
    public class NormalMode : ITorrentMode
    {
        /// <summary>
        /// Gets if we should request all peers for the same piece block.
        /// </summary>
        public bool RequestAllPeersForSameBlock
        {
            get { return false; }
        }
    }
}
