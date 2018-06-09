using System;
using TorrentSwifter;

namespace TorrentSwifterCLI
{
    class Program
    {
        const string DefaultTorrentPath = @"C:\Users\Whinarn\Downloads\Game.Of.Thrones.S01-S05.1080p.BluRay.x264-Scene [NO RAR].torrent";

        static void Main(string[] args)
        {
            string torrentFilePath = (args.Length > 0 ? args[0] : DefaultTorrentPath);

            var torrentInfo = new TorrentInfo();
            torrentInfo.LoadFromFile(torrentFilePath);

            Console.WriteLine("Info Hash: {0}", torrentInfo.InfoHash);
        }
    }
}
