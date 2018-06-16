﻿using System;

namespace TorrentSwifter.Helpers
{
    internal static class HexHelper
    {
        #region Consts
        private const string HexChars = "0123456789abcdef";
        #endregion

        #region Bytes To Hex
        /// <summary>
        /// Returns a hexadecimal string converted from a byte array.
        /// </summary>
        /// <param name="bytes">The byte array.</param>
        /// <returns>The hexadecimal string.</returns>
        public static string BytesToHex(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException("bytes");

            char[] chars = new char[bytes.Length * 2];
            for (int i = 0, j = 0; i < bytes.Length; i++, j += 2)
            {
                byte b = bytes[i];
                chars[j] = HexChars[b >> 4];
                chars[j + 1] = HexChars[b & 0xF];
            }
            return new string(chars);
        }

        /// <summary>
        /// Returns a hexadecimal string converted from a byte array.
        /// </summary>
        /// <param name="bytes">The byte array.</param>
        /// <param name="offset">The byte offset.</param>
        /// <param name="count">The byte count.</param>
        /// <returns>The hexadecimal string.</returns>
        public static string BytesToHex(byte[] bytes, int offset, int count)
        {
            if (bytes == null) throw new ArgumentNullException("bytes");
            else if (offset < 0 || offset >= bytes.Length) throw new ArgumentOutOfRangeException("offset");
            else if (count < 0 || (offset + count) > bytes.Length) throw new ArgumentOutOfRangeException("count");

            if (count == 0)
                return string.Empty;

            char[] chars = new char[count * 2];
            for (int i = 0, j = 0; i < count; i++, j += 2)
            {
                byte b = bytes[offset + i];
                chars[j] = HexChars[b >> 4];
                chars[j + 1] = HexChars[b & 0xF];
            }
            return new string(chars);
        }
        #endregion

        #region Hex To Bytes
        /// <summary>
        /// Returns a byte array converted from a hexadecimal string.
        /// </summary>
        /// <param name="hex">The hexadecimal string.</param>
        /// <returns>The byte array.</returns>
        public static byte[] HexToBytes(string hex)
        {
            if (hex == null) throw new ArgumentNullException("hex");
            else if ((hex.Length % 2) != 0) throw new ArgumentException("The hex length must be an even number.", "hex");

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0, j = 0; j < hex.Length; i++, j += 2)
            {
                char c1 = hex[j];
                char c2 = hex[j + 1];

                byte b;
                if (c1 >= '0' && c1 <= '9')
                {
                    b = (byte)((c1 - '0') << 4);
                }
                else if (c1 >= 'a' && c1 <= 'f')
                {
                    b = (byte)(((c1 - 'a') + 10) << 4);
                }
                else if (c1 >= 'A' && c1 <= 'F')
                {
                    b = (byte)(((c1 - 'A') + 10) << 4);
                }
                else
                {
                    throw new FormatException("Invalid hex character: " + c1);
                }

                if (c2 >= '0' && c2 <= '9')
                {
                    b |= (byte)(c2 - '0');
                }
                else if (c2 >= 'a' && c2 <= 'f')
                {
                    b |= (byte)((c2 - 'a') + 10);
                }
                else if (c2 >= 'A' && c2 <= 'F')
                {
                    b |= (byte)((c2 - 'A') + 10);
                }
                else
                {
                    throw new FormatException("Invalid hex character: " + c2);
                }

                bytes[i] = b;
            }

            return bytes;
        }
        #endregion
    }
}
