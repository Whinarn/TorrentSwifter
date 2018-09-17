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

namespace TorrentSwifter.Torrents.Modes
{
    /// <summary>
    /// End-game downloading mode.
    /// </summary>
    public class EndGameMode : TorrentModeBase
    {
        /// <summary>
        /// Gets if we should request all peers for the same piece block.
        /// </summary>
        public override bool RequestAllPeersForSameBlock
        {
            get { return true; }
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
