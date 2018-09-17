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
    /// Disk preferences.
    /// </summary>
    [Serializable]
    public sealed class DiskPreferences
    {
        #region Fields
        private int maxQueuedReads = 30;
        private int maxQueuedWrites = 20;

        private int maxConcurrentReads = 1;
        private int maxConcurrentWrites = 1;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the maximum count of queued reads from the disk.
        /// </summary>
        public int MaxQueuedReads
        {
            get { return maxQueuedReads; }
            set { maxQueuedReads = Math.Max(value, 1); }
        }

        /// <summary>
        /// Gets or sets the maximum count of queued writes to the disk.
        /// </summary>
        public int MaxQueuedWrites
        {
            get { return maxQueuedWrites; }
            set { maxQueuedWrites = Math.Max(value, 1); }
        }

        /// <summary>
        /// Gets or sets the maximum count of concurrent reads from the disk.
        /// </summary>
        public int MaxConcurrentReads
        {
            get { return maxConcurrentReads; }
            set { maxConcurrentReads = Math.Max(value, 1); }
        }

        /// <summary>
        /// Gets or sets the maximum count of concurrent writes to the disk.
        /// </summary>
        public int MaxConcurrentWrites
        {
            get { return maxConcurrentWrites; }
            set { maxConcurrentWrites = Math.Max(value, 1); }
        }
        #endregion
    }
}
