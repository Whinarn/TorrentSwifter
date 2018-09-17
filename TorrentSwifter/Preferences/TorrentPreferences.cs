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

namespace TorrentSwifter.Preferences
{
    /// <summary>
    /// Torrent preferences.
    /// </summary>
    [Serializable]
    public sealed class TorrentPreferences
    {
        #region Fields
        private bool allocateFullFileSizes = false;

        private long downloadBandwidthLimit = 0L;
        private long uploadBandwidthLimit = 0L;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets if the full file sizes are allocated for new torrent downloads.
        /// </summary>
        public bool AllocateFullFileSizes
        {
            get { return allocateFullFileSizes; }
            set { allocateFullFileSizes = value; }
        }

        /// <summary>
        /// Gets or sets the download bandwidth limit in bytes per second for the combined downloading from all active torrents.
        /// Zero means that no limit is imposed.
        /// </summary>
        public long DownloadBandwidthLimit
        {
            get { return downloadBandwidthLimit; }
            set { downloadBandwidthLimit = Math.Max(value, 0L); }
        }

        /// <summary>
        /// Gets or sets the upload bandwidth limit in bytes per second for the combined uploading from all active torrents.
        /// Zero means that no limit is imposed.
        /// </summary>
        public long UploadBandwidthLimit
        {
            get { return uploadBandwidthLimit; }
            set { uploadBandwidthLimit = Math.Max(value, 0L); }
        }
        #endregion
    }
}
