﻿using System;
using System.IO;
using TorrentSwifter;
using TorrentSwifter.Torrents;

namespace TorrentSwifterCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("You must pass at least one argument. Available commands: create, download");
                return;
            }

            try
            {
                string commandName = args[0];
                if (string.Equals(commandName, "create"))
                {
                    CreateTorrent(args);
                }
                else if (string.Equals(commandName, "download"))
                {
                    DownloadTorrent(args);
                }
                else
                {
                    Console.Error.WriteLine("The command is invalid: {0}\nAvailable commands: create, download", commandName);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("An exception occured: {0}", ex.ToString());
            }
        }

        private static void CreateTorrent(string[] args)
        {
            if (args.Length < 4)
            {
                Console.Error.WriteLine("You must pass at least 3 arguments to create a torrent: torrent name, directory path and the resulting torrent path");
                return;
            }

            string torrentName = args[1].Trim();
            string torrentDirPath = args[2].Trim();
            string torrentPath = args[3].Trim();

            torrentDirPath = Path.GetFullPath(torrentDirPath);
            torrentPath = Path.GetFullPath(torrentPath);

            if (string.IsNullOrEmpty(torrentName))
            {
                Console.Error.WriteLine("The torrent name cannot be empty.");
                return;
            }
            else if (!Directory.Exists(torrentDirPath) && !File.Exists(torrentDirPath))
            {
                Console.Error.WriteLine("The specified directory doesn't exist: {0}", torrentDirPath);
                return;
            }

            string parentDir = Path.GetDirectoryName(torrentPath);
            if (!Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }

            var torrentMetaData = new TorrentMetaData();
            torrentMetaData.Name = torrentName;
            torrentMetaData.Comment = "TorrentSwifter is the best, I tell you!";
            torrentMetaData.IsPrivate = true;
            torrentMetaData.Announces = new TorrentMetaData.AnnounceItem[]
            {
                new TorrentMetaData.AnnounceItem("http://tracker2.itzmx.com:6961/announce"),
                new TorrentMetaData.AnnounceItem("udp://tracker.coppersurfer.tk:6969/announce")
            };
            if (Directory.Exists(torrentDirPath))
            {
                torrentMetaData.SetDirectoryFiles(torrentDirPath);
            }
            else
            {
                torrentMetaData.SetSingleFile(torrentDirPath);
            }
            torrentMetaData.SaveToFile(torrentPath);

            Console.WriteLine("Created Torrent with info hash: {0}", torrentMetaData.InfoHash);
        }

        private static void DownloadTorrent(string[] args)
        {
            if (args.Length < 3)
            {
                Console.Error.WriteLine("You must pass at least 2 arguments to create a torrent: torrent path and download path");
                return;
            }

            string torrentPath = args[1].Trim();
            string downloadPath = args[2].Trim();

            torrentPath = Path.GetFullPath(torrentPath);
            downloadPath = Path.GetFullPath(downloadPath);

            if (!File.Exists(torrentPath))
            {
                Console.Error.WriteLine("The torrent doesn't exist: {0}", torrentPath);
                return;
            }

            var torrentMetaData = new TorrentMetaData();
            torrentMetaData.LoadFromFile(torrentPath);

            TorrentEngine.Initialize();
            try
            {
                var torrent = new Torrent(torrentMetaData, downloadPath);
                torrent.Start();

                while (true)
                {
                    var keyInfo = Console.ReadKey(true);
                    if (keyInfo.Key == ConsoleKey.Escape)
                        break;
                }

                torrent.Stop();
            }
            finally
            {
                TorrentEngine.Uninitialize();
            }
        }
    }
}
