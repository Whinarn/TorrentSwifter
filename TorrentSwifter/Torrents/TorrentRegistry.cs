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
        /// Stops all currently active torrents.
        /// </summary>
        public static void StopAllActiveTorrents()
        {
            var torrents = torrentList.ToArray();
            foreach (var torrent in torrents)
            {
                torrent.Stop();
            }
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
