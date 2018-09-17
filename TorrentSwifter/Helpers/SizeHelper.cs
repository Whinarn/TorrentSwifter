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

namespace TorrentSwifter.Helpers
{
    internal static class SizeHelper
    {
        public const long KiloByte = 1024;
        public const long MegaByte = 1024 * KiloByte;
        public const long GigaByte = 1024 * MegaByte;
        public const long TerraByte = 1024 * GigaByte;
        public const long PetaByte = 1024 * TerraByte;

        public static int GetRecommendedPieceSize(long totalSize)
        {
            if (totalSize >= (SizeHelper.GigaByte * 20))
            {
                return 4 * (int)SizeHelper.MegaByte;
            }
            else if (totalSize >= (SizeHelper.GigaByte * 6))
            {
                return 2 * (int)SizeHelper.MegaByte;
            }
            else if (totalSize >= (SizeHelper.GigaByte * 2))
            {
                return 1 * (int)SizeHelper.MegaByte;
            }
            else if (totalSize >= (SizeHelper.MegaByte * 512))
            {
                return 512 * (int)SizeHelper.KiloByte;
            }
            else if (totalSize >= (SizeHelper.MegaByte * 350))
            {
                return 256 * (int)SizeHelper.KiloByte;
            }
            else if (totalSize >= (SizeHelper.MegaByte * 150))
            {
                return 128 * (int)SizeHelper.KiloByte;
            }
            else if (totalSize >= (SizeHelper.MegaByte * 50))
            {
                return 64 * (int)SizeHelper.KiloByte;
            }
            else
            {
                return 32 * (int)SizeHelper.KiloByte;
            }
        }

        public static string GetHumanReadableSize(long size)
        {
            if (size < KiloByte)
            {
                return string.Format("{0} B", size);
            }
            else if (size < MegaByte)
            {
                return string.Format("{0:0.00} kB", (size / (double)KiloByte));
            }
            else if (size < GigaByte)
            {
                return string.Format("{0:0.00} MB", (size / (double)MegaByte));
            }
            else if (size < TerraByte)
            {
                return string.Format("{0:0.00} GB", (size / (double)GigaByte));
            }
            else if (size < PetaByte)
            {
                return string.Format("{0:0.00} TB", (size / (double)TerraByte));
            }
            else
            {
                return string.Format("{0:0.00} PB", (size / (double)PetaByte));
            }
        }

        public static string GetHumanReadableSpeed(long speed)
        {
            if (speed < KiloByte)
            {
                return string.Format("{0} B/s", speed);
            }
            else if (speed < MegaByte)
            {
                return string.Format("{0:0.00} kB/s", (speed / (double)KiloByte));
            }
            else if (speed < GigaByte)
            {
                return string.Format("{0:0.00} MB/s", (speed / (double)MegaByte));
            }
            else if (speed < TerraByte)
            {
                return string.Format("{0:0.00} GB/s", (speed / (double)GigaByte));
            }
            else if (speed < PetaByte)
            {
                return string.Format("{0:0.00} TB/s", (speed / (double)TerraByte));
            }
            else
            {
                return string.Format("{0:0.00} PB/s", (speed / (double)PetaByte));
            }
        }
    }
}
