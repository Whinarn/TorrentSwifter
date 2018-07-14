using System;

namespace TorrentSwifter.Helpers
{
    public static class SizeHelper
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

        public static string GetHumanReadableSpeed(long rate)
        {
            if (rate < KiloByte)
            {
                return string.Format("{0} B/s", rate);
            }
            else if (rate < MegaByte)
            {
                return string.Format("{0:0.00} kB/s", (rate / (double)KiloByte));
            }
            else if (rate < GigaByte)
            {
                return string.Format("{0:0.00} MB/s", (rate / (double)MegaByte));
            }
            else if (rate < TerraByte)
            {
                return string.Format("{0:0.00} GB/s", (rate / (double)GigaByte));
            }
            else if (rate < PetaByte)
            {
                return string.Format("{0:0.00} TB/s", (rate / (double)TerraByte));
            }
            else
            {
                return string.Format("{0:0.00} PB/s", (rate / (double)PetaByte));
            }
        }
    }
}
