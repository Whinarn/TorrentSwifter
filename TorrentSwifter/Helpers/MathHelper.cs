using System;

namespace TorrentSwifter.Helpers
{
    internal static class MathHelper
    {
        public static int GetNextPowerOfTwo(int value)
        {
            if (value == 0)
                value = 1;

            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            value++;
            return value;
        }

        public static bool IsPowerOfTwo(int value)
        {
            return ((value & (value - 1)) == 0);
        }

        public static int Clamp(int value, int min, int max)
        {
            return (value < min ? min : (value > max ? max : value));
        }
    }
}
