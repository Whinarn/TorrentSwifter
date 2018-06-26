using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

namespace TorrentSwifter.Helpers
{
    internal static class UriHelper
    {
        private const string HexChars = "0123456789abcdef";
        private static readonly char[] NotEncodedChars = { '-', '_', '.', '~' };

        public static string UrlEncodeText(string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            else if (text.Length == 0)
                return string.Empty;

            return WebUtility.UrlEncode(text);
        }

        public static string UrlEncodeText(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            else if (bytes.Length == 0)
                return string.Empty;

            byte[] encodedBytes = WebUtility.UrlEncodeToBytes(bytes, 0, bytes.Length);
            return Encoding.UTF8.GetString(encodedBytes);
        }

        public static string UrlEncodeText(byte[] bytes, int offset, int count)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            else if (offset < 0 || offset >= bytes.Length)
                throw new ArgumentOutOfRangeException("offset");
            else if (count < 0 || (offset + count) > bytes.Length)
                throw new ArgumentOutOfRangeException("count");
            else if (count == 0)
                return string.Empty;

            byte[] encodedBytes = WebUtility.UrlEncodeToBytes(bytes, offset, count);
            return Encoding.UTF8.GetString(encodedBytes);
        }

        public static byte[] UrlEncode(string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            else if (text.Length == 0)
                return new byte[0];

            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            return WebUtility.UrlEncodeToBytes(textBytes, 0, textBytes.Length);
        }

        public static byte[] UrlEncode(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            else if (bytes.Length == 0)
                return new byte[0];

            return WebUtility.UrlEncodeToBytes(bytes, 0, bytes.Length);
        }

        public static byte[] UrlEncode(byte[] bytes, int offset, int count)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            else if (offset < 0 || offset >= bytes.Length)
                throw new ArgumentOutOfRangeException("offset");
            else if (count < 0 || (offset + count) > bytes.Length)
                throw new ArgumentOutOfRangeException("count");
            else if (count == 0)
                return new byte[0];

            return WebUtility.UrlEncodeToBytes(bytes, offset, count);
        }

        public static string UrlDecodeText(string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            else if (text.Length == 0)
                return string.Empty;

            return WebUtility.UrlDecode(text);
        }

        public static string UrlDecodeText(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            else if (bytes.Length == 0)
                return string.Empty;

            byte[] decodedBytes = WebUtility.UrlDecodeToBytes(bytes, 0, bytes.Length);
            return Encoding.UTF8.GetString(decodedBytes);
        }

        public static string UrlDecodeText(byte[] bytes, int offset, int count)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            else if (offset < 0 || offset >= bytes.Length)
                throw new ArgumentOutOfRangeException("offset");
            else if (count < 0 || (offset + count) > bytes.Length)
                throw new ArgumentOutOfRangeException("count");
            else if (count == 0)
                return string.Empty;

            byte[] decodedBytes = WebUtility.UrlDecodeToBytes(bytes, offset, count);
            return Encoding.UTF8.GetString(decodedBytes);
        }

        public static byte[] UrlDecode(string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            else if (text.Length == 0)
                return new byte[0];

            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            return WebUtility.UrlDecodeToBytes(textBytes, 0, textBytes.Length);
        }

        public static byte[] UrlDecode(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            else if (bytes.Length == 0)
                return new byte[0];

            return WebUtility.UrlDecodeToBytes(bytes, 0, bytes.Length);
        }

        public static byte[] UrlDecode(byte[] bytes, int offset, int count)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            else if (offset < 0 || offset >= bytes.Length)
                throw new ArgumentOutOfRangeException("offset");
            else if (count < 0 || (offset + count) > bytes.Length)
                throw new ArgumentOutOfRangeException("count");
            else if (count == 0)
                return new byte[0];

            return WebUtility.UrlDecodeToBytes(bytes, offset, count);
        }

        public static Uri AppendQueryString(Uri uri, IEnumerable<KeyValuePair<string, object>> queryParameters)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");
            else if (queryParameters == null)
                throw new ArgumentNullException("queryParameters");

            var uriBuilder = new UriBuilder(uri);
            var queryString = new StringBuilder();

            string existingQueryString = uriBuilder.Query;
            if (!string.IsNullOrEmpty(existingQueryString) && existingQueryString.StartsWith("?"))
            {
                existingQueryString = existingQueryString.Substring(1);
                string[] queryStringSplit = existingQueryString.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < queryStringSplit.Length; i++)
                {
                    if (i > 0)
                    {
                        queryString.Append('&');
                    }

                    int equalsIndex = queryStringSplit[i].IndexOf('=');
                    if (equalsIndex != -1)
                    {
                        string queryKey = queryStringSplit[i].Substring(0, equalsIndex).Trim();
                        string queryValue = queryStringSplit[i].Substring(equalsIndex + 1).Trim();
                        queryString.Append(queryKey);
                        queryString.Append('=');
                        queryString.Append(queryValue);
                    }
                    else
                    {
                        queryString.Append(queryStringSplit[i].Trim());
                    }
                }
            }

            if (queryString.Length > 0)
            {
                // Append the ampersand if we had an existing querystring
                queryString.Append('&');
            }

            bool isFirst = true;
            foreach (var queryParameter in queryParameters)
            {
                if (!isFirst)
                {
                    queryString.Append('&');
                }
                else
                {
                    isFirst = false;
                }

                queryString.Append(queryParameter.Key);
                queryString.Append('=');

                object parameterValue = queryParameter.Value;
                if (parameterValue is string)
                {
                    queryString.Append(parameterValue as string);
                }
                else if (parameterValue is IConvertible)
                {
                    var parameterValueConv = (parameterValue as IConvertible);
                    queryString.Append(parameterValueConv.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    queryString.Append(parameterValue.ToString());
                }
            }

            uriBuilder.Query = queryString.ToString();
            return uriBuilder.Uri;
        }
    }
}
