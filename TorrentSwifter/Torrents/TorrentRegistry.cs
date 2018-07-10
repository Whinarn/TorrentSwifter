using System;
using System.Collections.Generic;

namespace TorrentSwifter.Torrents
{
    /// <summary>
    /// The registry over active torrents.
    /// </summary>
    public static class TorrentRegistry
    {
        private static List<Torrent> torrentList = new List<Torrent>();
        private static Dictionary<InfoHash, Torrent> torrentByInfoHash = new Dictionary<InfoHash, Torrent>();

        /// <summary>
        /// Returns an array of all currently active torrents.
        /// </summary>
        /// <returns>An array of torrents.</returns>
        public static Torrent[] GetAllActiveTorrents()
        {
            return torrentList.ToArray();
        }

        /// <summary>
        /// Attempts to find a torrent by its info hash.
        /// </summary>
        /// <param name="infoHash">The torrent info hash.</param>
        /// <returns>The found torrent, if any.</returns>
        public static Torrent FindTorrentByInfoHash(InfoHash infoHash)
        {
            Torrent torrent;
            if (torrentByInfoHash.TryGetValue(infoHash, out torrent))
                return torrent;
            else
                return null;
        }

        internal static void RegisterTorrent(Torrent torrent)
        {
            if (!torrentList.Contains(torrent))
            {
                torrentList.Add(torrent);
                torrentByInfoHash[torrent.InfoHash] = torrent;
            }
        }

        internal static void UnregisterTorrent(Torrent torrent)
        {
            if (torrentList.Remove(torrent))
            {
                torrentByInfoHash.Remove(torrent.InfoHash);
            }
        }
    }
}
