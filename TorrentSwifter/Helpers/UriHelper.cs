using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TorrentSwifter.Helpers
{
    internal static class UriHelper
    {
        private const string HexChars = "0123456789abcdef";
        private static readonly char[] NotEncodedChars = { '-', '_', '.', '~' };

        public static string UrlEncode(string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");

            byte[] bytes = Encoding.UTF8.GetBytes(text);
            return UrlEncode(bytes, 0, bytes.Length);
        }

        public static string UrlEncode(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");

            return UrlEncode(bytes, 0, bytes.Length);
        }

        public static string UrlEncode(byte[] bytes, int offset, int count)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            else if (offset < 0 || offset >= bytes.Length)
                throw new ArgumentOutOfRangeException("offset");
            else if (count < 0 || (offset + count) > bytes.Length)
                throw new ArgumentOutOfRangeException("count");
            else if (count == 0)
                return string.Empty;

            var sb = new StringBuilder(count * 3);

            for (int i = 0; i < count; i++)
            {
                byte b = bytes[offset + i];
                char c = (char)b;
                if (c == ' ')
                {
                    sb.Append('+');
                }
                else if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || NotEncodedChars.Contains(c))
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append('%');
                    sb.Append(HexChars[b >> 4]);
                    sb.Append(HexChars[b & 0xF]);
                }
            }

            return sb.ToString();
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
