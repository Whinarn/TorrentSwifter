using System;

namespace TorrentSwifter.Torrents
{
    /// <summary>
    /// Different states for torrents.
    /// </summary>
    public enum TorrentState
    {
        /// <summary>
        /// The torrent is inactive.
        /// </summary>
        Inactive,
        /// <summary>
        /// The torrent is checking the integrity of the already downloaded data.
        /// </summary>
        IntegrityChecking,
        /// <summary>
        /// The torrent is downloading.
        /// </summary>
        Downloading,
        /// <summary>
        /// The torrent is seeding. All data has already been downloaded.
        /// </summary>
        Seeding
    }
}
