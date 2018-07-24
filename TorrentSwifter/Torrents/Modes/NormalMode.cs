using System;

namespace TorrentSwifter.Torrents.Modes
{
    /// <summary>
    /// Normal downloading and uploading mode.
    /// </summary>
    public class NormalMode : TorrentModeBase
    {
        /// <summary>
        /// Gets if we should request all peers for the same piece block.
        /// </summary>
        public override bool RequestAllPeersForSameBlock
        {
            get { return false; }
        }

        /// <summary>
        /// Gets if we mask our bitmasks for connected peers, useful for maquerading as a leecher when for example super-seeding.
        /// Note that this will also disable the have-piece messages normally sent to peers once we have verified a new piece.
        /// </summary>
        public override bool MaskBitmasks
        {
            get { return false; }
        }
    }
}
