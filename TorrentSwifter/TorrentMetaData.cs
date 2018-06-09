using System;
using System.Collections.Generic;
using System.IO;
using TorrentSwifter.Encodings;
using TorrentSwifter.Helpers;

namespace TorrentSwifter
{
    /// <summary>
    /// Torrent meta-data information.
    /// </summary>
    public sealed class TorrentMetaData
    {
        #region Consts
        private const int MinPieceSize = 32 * (int)SizeHelper.KiloByte;
        private const int MaxPieceSize = 8 * (int)SizeHelper.MegaByte;
        #endregion

        #region Classes
        /// <summary>
        /// File item information contained within torrent information.
        /// </summary>
        public struct FileItem
        {
            private string path;
            private long size;
            private byte[] md5Hash;

            /// <summary>
            /// Gets the path of the file.
            /// </summary>
            public string Path
            {
                get { return path; }
            }

            /// <summary>
            /// Gets or sets the size of the file.
            /// </summary>
            public long Size
            {
                get { return size; }
                set { size = value; }
            }

            /// <summary>
            /// Gets or sets the MD5 hash of this file.
            /// </summary>
            public byte[] MD5Hash
            {
                get { return md5Hash; }
                set { md5Hash = value; }
            }

            /// <summary>
            /// Creates new file item information.
            /// </summary>
            /// <param name="path">The path of the file.</param>
            public FileItem(string path)
            {
                if (path == null)
                    throw new ArgumentNullException("path");

                this.path = path.Replace('\\', '/');
                this.size = 0L;
                this.md5Hash = null;
            }

            /// <summary>
            /// Creates new file item information.
            /// </summary>
            /// <param name="path">The path of the file.</param>
            /// <param name="size">The file size.</param>
            public FileItem(string path, long size)
            {
                if (path == null)
                    throw new ArgumentNullException("path");
                else if (size < 0L)
                    throw new ArgumentOutOfRangeException("The file sizes cannot be negative.", "size");

                this.path = path.Replace('\\', '/');
                this.size = size;
                this.md5Hash = null;
            }

            /// <summary>
            /// Creates new file item information.
            /// </summary>
            /// <param name="path">The path of the file.</param>
            /// <param name="size">The file size.</param>
            /// <param name="md5Hash">The MD5 hash of the file.</param>
            public FileItem(string path, long size, byte[] md5Hash)
            {
                if (path == null)
                    throw new ArgumentNullException("path");
                else if (size < 0L)
                    throw new ArgumentOutOfRangeException("The file sizes cannot be negative.", "size");
                else if (md5Hash != null && md5Hash.Length != 16)
                    throw new ArgumentException("The MD5 hash must be 16 bytes long.", "md5Hash");

                this.path = path.Replace('\\', '/');
                this.size = size;
                this.md5Hash = md5Hash;
            }
        }

        /// <summary>
        /// Announce item information ontained within torrent information.
        /// </summary>
        public struct AnnounceItem
        {
            private string[] urls;

            /// <summary>
            /// Gets the announce primary URL.
            /// </summary>
            public string Url
            {
                get { return (urls != null && urls.Length > 0 ? urls[0] : null); }
            }

            /// <summary>
            /// Gets the announce URLs.
            /// </summary>
            public string[] Urls
            {
                get { return urls; }
            }

            /// <summary>
            /// Creates new announce information.
            /// </summary>
            /// <param name="url">The announce URL.</param>
            public AnnounceItem(string url)
            {
                if (url == null)
                    throw new ArgumentNullException("url");
                else if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                    throw new ArgumentException("The URL is not of correct format.", "url");
                
                this.urls = new string[] { url };
            }

            /// <summary>
            /// Creates new announce information.
            /// </summary>
            /// <param name="urls">The announce URLs.</param>
            public AnnounceItem(string[] urls)
            {
                if (urls == null)
                    throw new ArgumentNullException("urls");
                else if (urls.Length == 0)
                    throw new ArgumentException("There must be at least one URL.", "urls");

                for (int i = 0; i < urls.Length; i++)
                {
                    if (urls[i] == null)
                        throw new ArgumentException(string.Format("The URL at index {0} is null.", i), "urls");
                    else if (!Uri.IsWellFormedUriString(urls[i], UriKind.Absolute))
                        throw new ArgumentException(string.Format("The URL at index {0} is not of correct format.", i), "url");
                }

                this.urls = urls;
            }
        }

        /// <summary>
        /// A piece hash.
        /// </summary>
        public struct PieceHash : IEquatable<PieceHash>, IEquatable<byte[]>
        {
            private readonly byte[] hash;
            private readonly string hashString;

            /// <summary>
            /// Gets the hash bytes.
            /// </summary>
            public byte[] Hash
            {
                get { return hash; }
            }

            /// <summary>
            /// Creates a new piece hash.
            /// </summary>
            /// <param name="hash">The hash bytes.</param>
            public PieceHash(byte[] hash)
            {
                if (hash == null)
                    throw new ArgumentNullException("hash");
                else if (hash.Length != 20)
                    throw new ArgumentException("The hash must be 20 bytes in size.", "hash");

                this.hash = hash;
                this.hashString = HexHelper.BytesToHex(hash);
            }

            /// <summary>
            /// Returns if this piece hash equals another object.
            /// </summary>
            /// <param name="obj">The other object.</param>
            /// <returns>If equals.</returns>
            public override bool Equals(object obj)
            {
                if (obj is PieceHash)
                    return Equals((PieceHash)obj);
                else
                    return false;
            }

            /// <summary>
            /// Returns if this piece hash equals another piece hash.
            /// </summary>
            /// <param name="other">The other piece hash.</param>
            /// <returns>If equals.</returns>
            public bool Equals(PieceHash other)
            {
                if (hash.Length != other.hash.Length)
                    return false;

                bool result = true;
                for (int i = 0; i < hash.Length; i++)
                {
                    if (hash[i] != other.hash[i])
                    {
                        result = false;
                        break;
                    }
                }
                return result;
            }

            /// <summary>
            /// Returns if this piece hash equals another piece hash.
            /// </summary>
            /// <param name="other">The other piece hash.</param>
            /// <returns>If equals.</returns>
            public bool Equals(byte[] other)
            {
                if (hash.Length != other.Length)
                    return false;

                bool result = true;
                for (int i = 0; i < hash.Length; i++)
                {
                    if (hash[i] != other[i])
                    {
                        result = false;
                        break;
                    }
                }
                return result;
            }

            /// <summary>
            /// Returns the hash code of this piece hash.
            /// </summary>
            /// <returns>The hash code.</returns>
            public override int GetHashCode()
            {
                int hashCode = 17;
                for (int i = 0; i < hash.Length; i++)
                {
                    hashCode = ((hashCode * 31) ^ hash[i]);
                }
                return hashCode;
            }

            /// <summary>
            /// Returns the text-representation of this piece hash.
            /// </summary>
            /// <returns>The hash in hexadecimals..</returns>
            public override string ToString()
            {
                return hashString;
            }
        }
        #endregion

        #region Fields
        private string name = null;
        private string comment = null;
        private string createdBy = null;
        private DateTime creationDate = DateTime.MinValue;
        private bool isPrivate = false;
        private string source = null;
        private PieceHash infoHash = default(PieceHash);

        private int pieceSize = 0;
        private PieceHash[] pieceHashes = null;

        private FileItem[] files = null;
        private AnnounceItem[] announces = null;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets name of the torrent.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// Gets or sets comment of the torrent.
        /// </summary>
        public string Comment
        {
            get { return comment; }
            set { comment = value; }
        }

        /// <summary>
        /// Gets or sets created by information of the torrent.
        /// </summary>
        public string CreatedBy
        {
            get { return createdBy; }
            set { createdBy = value; }
        }

        /// <summary>
        /// Gets or sets date of creation of the torrent.
        /// </summary>
        public DateTime CreationDate
        {
            get { return creationDate; }
            set { creationDate = value; }
        }

        /// <summary>
        /// Gets or sets if this torrent is private.
        /// </summary>
        public bool IsPrivate
        {
            get { return isPrivate; }
            set { isPrivate = value; }
        }

        /// <summary>
        /// Gets or sets the source of this torrent.
        /// </summary>
        public string Source
        {
            get { return source; }
            set { source = value; }
        }

        /// <summary>
        /// Gets the info hash of this torrent.
        /// </summary>
        public PieceHash InfoHash
        {
            get { return infoHash; }
        }

        /// <summary>
        /// Gets or sets the size of pieces in this torrent.
        /// </summary>
        public int PieceSize
        {
            get { return pieceSize; }
            set
            {
                if (value < MinPieceSize)
                    throw new ArgumentException(string.Format("The piece size cannot be less than {0} bytes ({1}).", MinPieceSize, SizeHelper.GetHumanReadableSize(MinPieceSize)), "value");
                else if (value > MaxPieceSize)
                    throw new ArgumentException(string.Format("The piece size cannot be more than {0} bytes ({1}).", MaxPieceSize, SizeHelper.GetHumanReadableSize(MinPieceSize)), "value");
                else if (!MathHelper.IsPowerOfTwo(value))
                    throw new ArgumentException("The piece size must be a power of two.", "value");
                else if (files != null)
                    throw new InvalidOperationException("Unable to change the piece size once the files have been set.");

                pieceSize = value;
            }
        }

        /// <summary>
        /// Gets the piece hashes of this torrent.
        /// </summary>
        public PieceHash[] PieceHashes
        {
            get { return pieceHashes; }
        }

        /// <summary>
        /// Gets the files of this torrent.
        /// </summary>
        public FileItem[] Files
        {
            get { return files; }
        }

        /// <summary>
        /// Gets or sets the announces of this torrent.
        /// </summary>
        public AnnounceItem[] Announces
        {
            get { return announces; }
            set { announces = value; }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates new torrent meta-data information.
        /// </summary>
        public TorrentMetaData()
        {
            string assemblyVersion = AssemblyHelper.GetAssemblyVersion(typeof(TorrentMetaData));
            createdBy = string.Format("TorrentSwifter/{0}", assemblyVersion);
            creationDate = DateTime.Now;
        }
        #endregion

        #region Public Methods
        #region Loading
        /// <summary>
        /// Loads torrent information from a stream.
        /// </summary>
        /// <param name="stream">The stream to load the torrent from.</param>
        public void LoadFromStream(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            else if (!stream.CanRead)
                throw new ArgumentException("The stream cannot be read from.", "stream");

            var torrentInfo = BEncoding.Decode(stream) as BEncoding.Dictionary;
            if (torrentInfo == null)
                throw new InvalidDataException("The specified file does not appear to include torrent information.");

            Load(torrentInfo);
        }

        /// <summary>
        /// Loads torrent information from a torrent file on disk.
        /// </summary>
        /// <param name="filePath">The path to the torrent file.</param>
        public void LoadFromFile(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException("filePath");

            var torrentInfo = BEncoding.DecodeFromFile(filePath) as BEncoding.Dictionary;
            if (torrentInfo == null)
                throw new InvalidDataException("The specified file does not appear to include torrent information.");

            Load(torrentInfo);
        }
        #endregion

        #region Saving
        /// <summary>
        /// Saves this torrent information to a stream.
        /// </summary>
        /// <param name="stream">The stream to save to.</param>
        public void SaveToStream(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            else if (!stream.CanWrite)
                throw new ArgumentException("The stream cannot be written to.", "stream");

            var torrentInfo = Save();
            BEncoding.Encode(torrentInfo, stream);
        }

        /// <summary>
        /// Saves this torrent information to a file on disk.
        /// </summary>
        /// <param name="filePath">The path to the file to create.</param>
        public void SaveToFile(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException("filePath");

            var torrentInfo = Save();
            BEncoding.EncodeToFile(torrentInfo, filePath);
        }
        #endregion

        #region Set Files
        /// <summary>
        /// Assigns a single file for this torrent.
        /// </summary>
        /// <param name="filePath">The path to the single file for this torrent.</param>
        public void SetSingleFile(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException("filePath");

            filePath = Path.GetFullPath(filePath);
            if (!File.Exists(filePath))
                throw new FileNotFoundException(string.Format("The file could not be found: {0}", filePath), filePath);

            string parentDirectoryPath = IOHelper.GetParentDirectory(filePath);
            string fileName = Path.GetFileName(filePath);

            var files = new FileItem[]
            {
                new FileItem(fileName)
            };
            SetFiles(parentDirectoryPath, files);
        }

        /// <summary>
        /// Assigns multiple files inside of a directory for this torrent.
        /// </summary>
        /// <param name="directoryPath">The path to the directory.</param>
        public void SetDirectoryFiles(string directoryPath)
        {
            if (directoryPath == null)
                throw new ArgumentNullException("directoryPath");

            directoryPath = Path.GetFullPath(directoryPath);
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException(string.Format("The directory could not be found: {0}", directoryPath));

            string[] directoryFilePaths = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            if (directoryFilePaths.Length == 0)
                throw new InvalidOperationException(string.Format("The directory is empty.", directoryPath));

            var files = new FileItem[directoryFilePaths.Length];
            for (int i = 0; i < files.Length; i++)
            {
                string filePath = directoryFilePaths[i];
                string localPath = IOHelper.GetLocalPath(filePath, directoryPath);
                files[i] = new FileItem(localPath);
            }

            SetFiles(directoryPath, files);
        }

        /// <summary>
        /// Assigns the files for this torrent.
        /// </summary>
        /// <param name="rootPath">The path to the root directory of where the files can be found.</param>
        /// <param name="files">The files of the torrent.</param>
        public void SetFiles(string rootPath, FileItem[] files)
        {
            if (rootPath == null)
                throw new ArgumentNullException("rootPath");
            else if (!Directory.Exists(rootPath))
                throw new DirectoryNotFoundException(string.Format("The directory could not be found: {0}", rootPath));
            else if (files == null)
                throw new ArgumentNullException("files");
            else if (files.Length == 0)
                throw new ArgumentException("There must be at least one file.", "files");

            long totalSize = 0L;
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].Path == null)
                    throw new ArgumentException(string.Format("The file path at index {0} is null.", i), "files");

                string fileFullPath = IOHelper.GetTorrentFilePath(rootPath, files[i]);
                var fileInfo = new FileInfo(fileFullPath);
                if (!fileInfo.Exists)
                    throw new FileNotFoundException(string.Format("Unable to find the file: {0}", fileFullPath), fileFullPath);

                long fileSize = fileInfo.Length;
                files[i].Size = fileSize;
                totalSize += fileSize;
            }

            if (totalSize == 0L)
                throw new InvalidOperationException("Unable to create a torrent with the total size of zero.");

            if (pieceSize == 0)
                pieceSize = SizeHelper.GetRecommendedPieceSize(totalSize);

            int pieceCount = (int)((totalSize - 1) / pieceSize) + 1;
            pieceHashes = new PieceHash[pieceCount];

            int lastPieceSize = (int)(totalSize % pieceSize);
            if (lastPieceSize == 0)
                lastPieceSize = pieceSize;

            byte[] pieceData = new byte[pieceSize];
            int pieceDataOffset = 0;
            int pieceIndex = 0;
            int currentPieceSize = (pieceCount > 1 ? pieceSize : lastPieceSize);
            for (int i = 0; i < files.Length; i++)
            {
                string fileFullPath = IOHelper.GetTorrentFilePath(rootPath, files[i]);
                using (var fileStream = File.OpenRead(fileFullPath))
                {
                    while (pieceIndex < pieceCount)
                    {
                        int pieceRemaining = currentPieceSize - pieceDataOffset;
                        if (pieceRemaining <= 0)
                        {
                            byte[] pieceHash = HashHelper.ComputeSHA1(pieceData, 0, pieceDataOffset);
                            pieceHashes[pieceIndex] = new PieceHash(pieceHash);

                            ++pieceIndex;
                            pieceDataOffset = 0;
                            currentPieceSize = (pieceIndex < (pieceCount - 1) ? pieceSize : lastPieceSize);
                            pieceRemaining = currentPieceSize;

                            if (pieceIndex >= pieceCount)
                                break;
                        }

                        int readBytes = fileStream.Read(pieceData, pieceDataOffset, pieceRemaining);
                        if (readBytes <= 0)
                            break;

                        pieceDataOffset += readBytes;
                    }
                }
            }

            if (pieceDataOffset > 0)
            {
                byte[] pieceHash = HashHelper.ComputeSHA1(pieceData, 0, pieceDataOffset);
                pieceHashes[pieceIndex] = new PieceHash(pieceHash);
                ++pieceIndex;
            }

            if (pieceIndex < pieceCount)
                throw new InvalidOperationException(string.Format("Something went wrong when calculating piece hashes. Expected {0} hashes but only calculated {1}.", pieceCount, pieceIndex));

            this.files = files;
        }
        #endregion
        #endregion

        #region Private Methods
        private void ClearInfo()
        {
            name = null;
            comment = null;
            createdBy = null;
            creationDate = DateTime.MinValue;
            isPrivate = false;
            source = null;
            infoHash = default(PieceHash);

            pieceSize = 0;
            pieceHashes = null;

            files = null;
            announces = null;
        }

        private void Load(BEncoding.Dictionary torrentInfo)
        {
            ClearInfo();

            BEncoding.List announceList;
            if (torrentInfo.TryGetList("announce-list", out announceList))
            {
                int announceCount = announceList.Count;
                var newAnnounceList = new List<AnnounceItem>(announceCount);
                for (int i = 0; i < announceCount; i++)
                {
                    var urlList = (announceList[i] as BEncoding.List);
                    if (urlList != null)
                    {
                        int urlCount = urlList.Count;
                        var newUrlList = new List<string>(urlCount);
                        for (int j = 0; j < urlCount; j++)
                        {
                            string url = urlList.GetString(j);
                            if (!string.IsNullOrEmpty(url))
                            {
                                newUrlList.Add(url);
                            }
                        }

                        if (newUrlList.Count > 0)
                        {
                            newAnnounceList.Add(new AnnounceItem(newUrlList.ToArray()));
                        }
                    }
                }

                announces = newAnnounceList.ToArray();
            }
            else
            {
                string announceUrl;
                if (torrentInfo.TryGetString("announce", out announceUrl))
                {
                    announces = new AnnounceItem[]
                    {
                        new AnnounceItem(announceUrl)
                    };
                }
            }

            long creationDateTimestamp;
            if (!torrentInfo.TryGetString("comment", out comment))
            {
                comment = null;
            }
            if (!torrentInfo.TryGetString("created by", out createdBy))
            {
                createdBy = null;
            }
            if (torrentInfo.TryGetInteger("creation date", out creationDateTimestamp))
            {
                creationDate = TimeHelper.GetDateFromUnixTimestamp(creationDateTimestamp);
            }
            else
            {
                creationDate = DateTime.MinValue;
            }

            BEncoding.Dictionary info;
            if (!torrentInfo.TryGetDictionary("info", out info))
                throw new InvalidDataException("The info dictionary is missing in the torrent meta-data.");

            infoHash = ComputeInfoHash(info);

            long pieceSize;
            if (!info.TryGetInteger("piece length", out pieceSize))
                throw new InvalidDataException("The piece length is missing in the torrent meta-data. But it is required for all torrents.");

            byte[] pieceHashData;
            if (!info.TryGetByteArray("pieces", out pieceHashData))
                throw new InvalidDataException("The piece hashes are missing in the torrent meta-data. But it is required for all torrents.");
            else if ((pieceHashData.Length % 20) != 0)
                throw new InvalidDataException("The piece hashes are invalid in the torrent meta-data. It must be a multiple of 20 (for SHA1 hashes).");

            int pieceCount = (pieceHashData.Length / 20);
            pieceHashes = new PieceHash[pieceCount];
            for (int i = 0; i < pieceCount; i++)
            {
                byte[] pieceHashBytes = new byte[20];
                Buffer.BlockCopy(pieceHashData, (i * 20), pieceHashBytes, 0, 20);
                pieceHashes[i] = new PieceHash(pieceHashBytes);
            }

            long isPrivate;
            if (info.TryGetInteger("private", out isPrivate) && isPrivate == 1)
            {
                this.isPrivate = true;
            }

            if (!info.TryGetString("source", out source))
            {
                source = null;
            }

            if (!info.TryGetString("name", out name) || string.IsNullOrEmpty(name))
                throw new InvalidDataException("The name string is missing in the torrent meta-data. But it is required for all torrents.");

            this.pieceSize = (int)pieceSize;

            BEncoding.List fileList;
            if (info.TryGetList("files", out fileList))
            {
                int fileCount = fileList.Count;
                files = new FileItem[fileCount];
                for (int i = 0; i < fileCount; i++)
                {
                    var fileItem = fileList[i] as BEncoding.Dictionary;
                    if (fileItem == null)
                        throw new InvalidDataException("The format of the torrent meta-data is invalid. Expected a dictionary of file information.");

                    long fileSize;
                    if (!fileItem.TryGetInteger("length", out fileSize))
                        throw new InvalidDataException("The format of the torrent meta-data is invalid. Expected a file size.");

                    string fileMD5HashHex;
                    byte[] fileMD5Hash = null;
                    if (fileItem.TryGetString("md5sum", out fileMD5HashHex) && fileMD5HashHex.Length == 32)
                    {
                        fileMD5Hash = HexHelper.HexToBytes(fileMD5HashHex);
                    }

                    BEncoding.List pathList;
                    if (!fileItem.TryGetList("path", out pathList))
                        throw new InvalidDataException("The format of the torrent meta-data is invalid. Expected a file path.");

                    int pathPartCount = pathList.Count;
                    string[] pathParts = new string[pathPartCount];
                    for (int j = 0; j < pathPartCount; j++)
                    {
                        string pathPart = pathList.GetString(j);
                        if (string.IsNullOrEmpty(pathPart))
                            throw new InvalidDataException("The format of the torrent meta-data is invalid. Expected a file path.");

                        pathParts[j] = pathPart;
                    }

                    string filePath = string.Join("/", pathParts);
                    files[i] = new FileItem(filePath, fileSize, fileMD5Hash);
                }
            }
            else
            {
                long fileSize;
                if (!info.TryGetInteger("length", out fileSize))
                    throw new InvalidDataException("The file length is missing in the torrent meta-data. But it is required for single-file torrents.");

                string fileMD5HashHex;
                byte[] fileMD5Hash = null;
                if (info.TryGetString("md5sum", out fileMD5HashHex) && fileMD5HashHex.Length == 32)
                {
                    fileMD5Hash = HexHelper.HexToBytes(fileMD5HashHex);
                }

                files = new FileItem[]
                {
                    new FileItem(name, fileSize, fileMD5Hash)
                };
            }

            long totalSize = ComputeTotalSize();
            int expectedPieceCount = (int)((totalSize - 1) / pieceSize) + 1;
            if (expectedPieceCount != pieceHashes.Length)
                throw new InvalidOperationException(string.Format("The calculated number of pieces is {0} while there are {1} piece hashes.", expectedPieceCount, pieceHashes.Length));
        }

        private BEncoding.Dictionary Save()
        {
            var torrentInfo = new BEncoding.Dictionary();
            long totalSize = ComputeTotalSize();

            // Make sure that there is a piece size
            if (pieceSize == 0)
                throw new InvalidOperationException("No piece size has been set yet.");
            else if (!MathHelper.IsPowerOfTwo(pieceSize))
                throw new InvalidOperationException("The piece size must be a power of two.");
            else if (files == null || files.Length == 0)
                throw new InvalidOperationException("No files have been defined yet.");
            else if (totalSize == 0L)
                throw new InvalidOperationException("The total size of the torrent cannot be 0.");

            int pieceCount = (int)((totalSize - 1) / pieceSize) + 1;
            if (pieceCount != pieceHashes.Length)
                throw new InvalidOperationException(string.Format("The calculated number of pieces is {0} while there are {1} piece hashes.", pieceCount, pieceHashes.Length));

            if (announces != null && announces.Length > 0)
            {
                if (!string.IsNullOrEmpty(announces[0].Url))
                {
                    torrentInfo.Add("announce", announces[0].Url);
                }

                var trackerList = new BEncoding.List();
                for (int i = 0; i < announces.Length; i++)
                {
                    var urls = announces[i].Urls;
                    if (urls == null || urls.Length == 0)
                        continue;

                    var urlList = new BEncoding.List();
                    for (int j = 0; j < urls.Length; j++)
                    {
                        if (!string.IsNullOrEmpty(urls[j]))
                        {
                            urlList.Add(urls[j]);
                        }
                    }

                    if (urlList.Count > 0)
                    {
                        trackerList.Add(urlList);
                    }
                }

                if (trackerList.Count > 0)
                {
                    torrentInfo.Add("announce-list", trackerList);
                }
            }

            if (!string.IsNullOrEmpty(comment))
            {
                torrentInfo.Add("comment", comment);
            }
            if (!string.IsNullOrEmpty(createdBy))
            {
                torrentInfo.Add("created by", createdBy);
            }
            if (creationDate != DateTime.MinValue)
            {
                long creationDateTimestamp = TimeHelper.GetUnixTimestampFromDate(creationDate);
                torrentInfo.Add("creation date", creationDateTimestamp);
            }

            // Get the piece hash data
            byte[] pieceHashData = new byte[20 * pieceCount];
            for (int i = 0; i < pieceCount; i++)
            {
                Buffer.BlockCopy(pieceHashes[i].Hash, 0, pieceHashData, (i * 20), 20);
            }

            var info = new BEncoding.Dictionary();
            info.Add("piece length", pieceSize);
            info.Add("pieces", pieceHashData);
            info.Add("private", (isPrivate ? 1 : 0));

            if (!string.IsNullOrEmpty(source))
            {
                info.Add("source", source);
            }

            if (files != null)
            {
                if (files.Length == 1)
                {
                    var fileItem = files[0];
                    info.Add("length", fileItem.Size);

                    if (fileItem.MD5Hash != null && fileItem.MD5Hash.Length == 16)
                    {
                        string fileMD5HashHex = HexHelper.BytesToHex(fileItem.MD5Hash);
                        info.Add("md5sum", fileMD5HashHex);
                    }

                    info.Add("name", GetFileName(fileItem.Path));
                }
                else if (files.Length > 1)
                {
                    info.Add("name", name ?? "Unnamed");

                    var fileList = new BEncoding.List();
                    for (int i = 0; i < files.Length; i++)
                    {
                        var fileItem = files[i];
                        var fileDictionary = new BEncoding.Dictionary();
                        fileDictionary.Add("length", fileItem.Size);

                        if (fileItem.MD5Hash != null && fileItem.MD5Hash.Length == 16)
                        {
                            string fileMD5HashHex = HexHelper.BytesToHex(fileItem.MD5Hash);
                            info.Add("md5sum", fileMD5HashHex);
                        }

                        string[] pathParts = fileItem.Path.Split(new char[] { '/' });
                        var pathList = new BEncoding.List(pathParts);
                        fileDictionary.Add("path", pathList);
                        fileList.Add(fileDictionary);
                    }

                    info.Add("files", fileList);
                }
            }

            infoHash = ComputeInfoHash(info);
            torrentInfo.Add("info", info);
            return torrentInfo;
        }

        private long ComputeTotalSize()
        {
            long totalSize = 0L;

            if (files != null)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    totalSize += files[i].Size;
                }
            }

            return totalSize;
        }

        private static PieceHash ComputeInfoHash(BEncoding.Dictionary info)
        {
            using (var stream = new MemoryStream())
            {
                BEncoding.Encode(info, stream);

                stream.Seek(0L, SeekOrigin.Begin);
                byte[] hash = HashHelper.ComputeSHA1(stream);
                return new PieceHash(hash);
            }
        }

        private static string GetFileName(string filePath)
        {
            int lastSlashIndex = filePath.LastIndexOf('/');
            if (lastSlashIndex != -1)
            {
                return filePath.Substring(lastSlashIndex + 1);
            }
            else
            {
                return filePath;
            }
        }
        #endregion
    }
}
