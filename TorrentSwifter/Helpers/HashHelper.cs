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
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace TorrentSwifter.Helpers
{
    internal static class HashHelper
    {
        private static readonly ThreadLocal<MD5> md5 = new ThreadLocal<MD5>(() => MD5.Create());
        private static readonly ThreadLocal<SHA1> sha1 = new ThreadLocal<SHA1>(() => SHA1.Create());

        public static byte[] ComputeMD5(byte[] data)
        {
            return md5.Value.ComputeHash(data);
        }

        public static byte[] ComputeMD5(byte[] data, int offset, int count)
        {
            return md5.Value.ComputeHash(data, offset, count);
        }

        public static byte[] ComputeMD5(Stream stream)
        {
            return md5.Value.ComputeHash(stream);
        }

        public static byte[] ComputeFileMD5(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                return md5.Value.ComputeHash(stream);
            }
        }

        public static byte[] ComputeSHA1(byte[] data)
        {
            return sha1.Value.ComputeHash(data);
        }

        public static byte[] ComputeSHA1(byte[] data, int offset, int count)
        {
            return sha1.Value.ComputeHash(data, offset, count);
        }

        public static byte[] ComputeSHA1(Stream stream)
        {
            return sha1.Value.ComputeHash(stream);
        }

        public static byte[] ComputeFileSHA1(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                return sha1.Value.ComputeHash(stream);
            }
        }
    }
}
