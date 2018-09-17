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
using System.Collections.Generic;

namespace TorrentSwifter.Collections
{
    // NOTE: This class does not care about validation, so avoid using this for dealing with HTTP headers
    internal sealed class HeaderCollection
    {
        #region Fields
        private Dictionary<string, object> headers = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        #endregion

        #region Properties
        public int Count
        {
            get { return headers.Count; }
        }

        public IEnumerable<string> Keys
        {
            get { return headers.Keys; }
        }
        #endregion

        #region Public Methods
        public void Add(string headerName, string headerValue)
        {
            if (headerName == null)
                throw new ArgumentNullException("headerName");
            else if (headerValue == null)
                throw new ArgumentNullException("headerValue");

            headerName = headerName.Trim();
            headerValue = headerValue.Trim();

            object existingValue;
            if (headers.TryGetValue(headerName, out existingValue))
            {
                headers[headerName] = MergeValues(headerValue, existingValue);
            }
            else
            {
                headers.Add(headerName, headerValue);
            }
        }

        public bool TryGetString(string headerName, out string headerValue)
        {
            object existingValue;
            if (headers.TryGetValue(headerName, out existingValue))
            {
                var existingList = (existingValue as List<string>);
                if (existingList != null && existingList.Count > 0)
                {
                    headerValue = existingList[0];
                    return true;
                }
                else
                {
                    string existingStringValue = (existingValue as string);
                    if (existingStringValue != null)
                    {
                        headerValue = existingStringValue;
                        return true;
                    }
                }
            }

            headerValue = null;
            return false;
        }

        public bool TryGetStrings(string headerName, out string[] headerValues)
        {
            object existingValue;
            if (headers.TryGetValue(headerName, out existingValue))
            {
                var existingList = (existingValue as List<string>);
                if (existingList != null)
                {
                    headerValues = existingList.ToArray();
                    return true;
                }
                else
                {
                    string existingStringValue = (existingValue as string);
                    if (existingStringValue != null)
                    {
                        headerValues = new string[] { existingStringValue };
                        return true;
                    }
                }
            }

            headerValues = null;
            return false;
        }
        #endregion

        #region Private Methods
        private static List<string> MergeValues(string headerValue, object existingValue)
        {
            var existingList = (existingValue as List<string>);
            if (existingList != null)
            {
                existingList.Add(headerValue);
                return existingList;
            }
            else
            {
                string existingStringValue = (existingValue as string);
                if (existingStringValue != null)
                {
                    var newList = new List<string>();
                    newList.Add(existingStringValue);
                    newList.Add(headerValue);
                    return newList;
                }
                else
                {
                    throw new InvalidOperationException("Attempted to merge two values, but the existing value was neither a string list or a string.");
                }
            }
        }
        #endregion
    }
}
