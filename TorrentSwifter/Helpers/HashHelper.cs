using System;
using System.IO;
using System.Security.Cryptography;

namespace TorrentSwifter.Helpers
{
    internal static class HashHelper
    {
        private static readonly MD5 md5 = MD5.Create();
        private static readonly SHA1 sha1 = SHA1.Create();

        public static byte[] ComputeMD5(byte[] data)
        {
            return md5.ComputeHash(data);
        }

        public static byte[] ComputeMD5(byte[] data, int offset, int count)
        {
            return md5.ComputeHash(data, offset, count);
        }

        public static byte[] ComputeMD5(Stream stream)
        {
            return md5.ComputeHash(stream);
        }

        public static byte[] ComputeFileMD5(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                return md5.ComputeHash(stream);
            }
        }

        public static byte[] ComputeSHA1(byte[] data)
        {
            return sha1.ComputeHash(data);
        }

        public static byte[] ComputeSHA1(byte[] data, int offset, int count)
        {
            return sha1.ComputeHash(data, offset, count);
        }

        public static byte[] ComputeSHA1(Stream stream)
        {
            return sha1.ComputeHash(stream);
        }

        public static byte[] ComputeFileSHA1(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                return sha1.ComputeHash(stream);
            }
        }
    }
}
