using System;

namespace TorrentSwifter.Torrents.Modes
{
    /// <summary>
    /// A torrent mode base.
    /// </summary>
    public abstract class TorrentModeBase : ITorrentMode
    {
        /// <summary>
        /// The torrent that this mode is assigned with.
        /// </summary>
        protected Torrent torrent = null;

        /// <summary>
        /// Gets or sets the torrent that this mode is assigned with.
        /// </summary>
        public Torrent Torrent
        {
            get { return torrent; }
            set
            {
                if (torrent == value)
                    return;

                torrent = value;
                OnTorrentChanged();
            }
        }

        /// <summary>
        /// Gets if we should request all peers for the same piece block.
        /// </summary>
        public abstract bool RequestAllPeersForSameBlock { get; }

        /// <summary>
        /// Gets if we mask our bitmasks for connected peers, useful for maquerading as a leecher when for example super-seeding.
        /// Note that this will also disable the have-piece messages normally sent to peers once we have verified a new piece.
        /// </summary>
        public abstract bool MaskBitmasks { get; }

        /// <summary>
        /// Called when the assigned torrent has changed.
        /// </summary>
        protected virtual void OnTorrentChanged()
        {

        }

        /// <summary>
        /// Updates this mode.
        /// </summary>
        public virtual void Update()
        {

        }
    }
}
