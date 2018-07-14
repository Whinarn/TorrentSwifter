﻿using System;
using System.Collections.Generic;

namespace TorrentSwifter.Helpers
{
    internal static class RandomHelper
    {
        private static readonly Random random = new Random();

        public static int Next()
        {
            lock (random)
            {
                return random.Next();
            }
        }

        public static int Next(int maxValue)
        {
            lock (random)
            {
                return random.Next(maxValue);
            }
        }

        public static double NextDouble()
        {
            lock (random)
            {
                return random.NextDouble();
            }
        }

        public static void Randomize<T>(List<T> list)
        {
            int listCount = list.Count;
            if (listCount < 2)
                return;

            for (int index = 0; index < listCount; index++)
            {
                int randomIndex = Next(listCount);
                if (randomIndex != index)
                {
                    T value = list[randomIndex];
                    list[randomIndex] = list[index];
                    list[index] = value;
                }
            }
        }
    }
}
