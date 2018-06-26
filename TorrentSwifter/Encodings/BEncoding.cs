using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace TorrentSwifter.Encodings
{
    /// <summary>
    /// BEncoding support.
    /// </summary>
    public static class BEncoding
    {
        #region Consts
        private const byte DictionaryByte = (byte)'d';
        private const byte ListByte = (byte)'l';
        private const byte IntegerByte = (byte)'i';
        private const byte EndByte = (byte)'e';
        private const byte StringLengthDivideByte = (byte)':';
        #endregion

        #region Classes
        /// <summary>
        /// A decoded Bencoded dictionary.
        /// </summary>
        public class Dictionary : Dictionary<string, object>
        {
            /// <summary>
            /// Creates a new dictionary.
            /// </summary>
            public Dictionary()
            {

            }

            /// <summary>
            /// Creates a new dictionary.
            /// </summary>
            /// <param name="capacity">The initial dictionary capacity.</param>
            public Dictionary(int capacity)
                : base(capacity)
            {

            }

            /// <summary>
            /// Creates a new dictionary.
            /// </summary>
            /// <param name="dictionary">The initial dictionary contents.</param>
            public Dictionary(IDictionary<string, object> dictionary)
                : base(dictionary)
            {

            }

            /// <summary>
            /// Attempts to get an integer item from the dictionary.
            /// </summary>
            /// <param name="key">The item key.</param>
            /// <param name="result">The output item value.</param>
            /// <returns>If the item was found.</returns>
            public bool TryGetInteger(string key, out long result)
            {
                object value;
                if (this.TryGetValue(key, out value) && (value is long))
                {
                    result = (long)value;
                    return true;
                }
                else
                {
                    result = 0L;
                    return false;
                }
            }

            /// <summary>
            /// Attempts to get an byte array item from the dictionary.
            /// </summary>
            /// <param name="key">The item key.</param>
            /// <param name="result">The output item value.</param>
            /// <returns>If the item was found.</returns>
            public bool TryGetByteArray(string key, out byte[] result)
            {
                object value;
                if (this.TryGetValue(key, out value) && (value is byte[]))
                {
                    result = (value as byte[]);
                    return true;
                }
                else
                {
                    result = null;
                    return false;
                }
            }

            /// <summary>
            /// Attempts to get a string item from the dictionary.
            /// </summary>
            /// <param name="key">The item key.</param>
            /// <param name="result">The output item value.</param>
            /// <returns>If the item was found.</returns>
            public bool TryGetString(string key, out string result)
            {
                object value;
                if (this.TryGetValue(key, out value) && (value is byte[]))
                {
                    byte[] stringBytes = (value as byte[]);
                    result = Encoding.UTF8.GetString(stringBytes);
                    return true;
                }
                else
                {
                    result = null;
                    return false;
                }
            }

            /// <summary>
            /// Attempts to get a dictionary item from the dictionary.
            /// </summary>
            /// <param name="key">The item key.</param>
            /// <param name="result">The output item value.</param>
            /// <returns>If the item was found.</returns>
            public bool TryGetDictionary(string key, out Dictionary result)
            {
                object value;
                if (this.TryGetValue(key, out value) && (value is Dictionary))
                {
                    result = (value as Dictionary);
                    return true;
                }
                else
                {
                    result = null;
                    return false;
                }
            }

            /// <summary>
            /// Attempts to get a list item from the dictionary.
            /// </summary>
            /// <param name="key">The item key.</param>
            /// <param name="result">The output item value.</param>
            /// <returns>If the item was found.</returns>
            public bool TryGetList(string key, out List result)
            {
                object value;
                if (this.TryGetValue(key, out value) && (value is List))
                {
                    result = (value as List);
                    return true;
                }
                else
                {
                    result = null;
                    return false;
                }
            }
        }

        /// <summary>
        /// A decoded Bencoded list.
        /// </summary>
        public class List : List<object>
        {
            /// <summary>
            /// Creates a new list.
            /// </summary>
            public List()
            {

            }

            /// <summary>
            /// Creates a new list.
            /// </summary>
            /// <param name="capacity">The initial list capacity.</param>
            public List(int capacity)
                : base(capacity)
            {

            }

            /// <summary>
            /// Creates a new list.
            /// </summary>
            /// <param name="collection">The initial list collection.</param>
            public List(IEnumerable<object> collection)
                : base(collection)
            {

            }

            /// <summary>
            /// Returns an integer item from the list at a specific index.
            /// </summary>
            /// <param name="index">The index of the item.</param>
            /// <returns>The integer item.</returns>
            public long GetInteger(int index)
            {
                object item = this[index];
                if (item is long)
                {
                    return (long)item;
                }
                else
                {
                    return 0L;
                }
            }

            /// <summary>
            /// Returns a byte array item from the list at a specific index.
            /// </summary>
            /// <param name="index">The index of the item.</param>
            /// <returns>The byte array item.</returns>
            public byte[] GetByteArray(int index)
            {
                object item = this[index];
                return (item as byte[]);
            }

            /// <summary>
            /// Returns a string item from the list at a specific index.
            /// </summary>
            /// <param name="index">The index of the item.</param>
            /// <returns>The string item.</returns>
            public string GetString(int index)
            {
                object item = this[index];
                byte[] stringBytes = (item as byte[]);
                if (stringBytes != null)
                {
                    return Encoding.UTF8.GetString(stringBytes);
                }
                else
                {
                    return null;
                }
            }

            /// <summary>
            /// Returns a list item from the list at a specific index.
            /// </summary>
            /// <param name="index">The index of the item.</param>
            /// <returns>The list item.</returns>
            public List GetList(int index)
            {
                object item = this[index];
                return (item as List);
            }

            /// <summary>
            /// Returns a dictionary item from the list at a specific index.
            /// </summary>
            /// <param name="index">The index of the item.</param>
            /// <returns>The dictionary item.</returns>
            public Dictionary GetDictionary(int index)
            {
                object item = this[index];
                return (item as Dictionary);
            }
        }

        private class Reader
        {
            private readonly byte[] data;
            private int offset;

            public int Offset
            {
                get { return offset; }
            }

            public bool EndOfStream
            {
                get { return (offset >= data.Length); }
            }

            public Reader(byte[] data, int offset)
            {
                this.data = data;
                this.offset = offset;
            }

            public byte Peek()
            {
                if (offset >= data.Length)
                    throw new EndOfStreamException("There are no more bytes to be read.");

                return data[offset];
            }

            public byte Pop()
            {
                if (offset >= data.Length)
                    throw new EndOfStreamException("There are no more bytes to be read.");

                return data[offset++];
            }

            public int Read(byte[] buffer, int offset, int count)
            {
                if (buffer == null)
                    throw new ArgumentNullException("buffer");
                else if (offset < 0 || offset >= buffer.Length)
                    throw new ArgumentOutOfRangeException("offset");
                else if (count < 0 || (offset + count) > buffer.Length)
                    throw new ArgumentOutOfRangeException("count");
                else if (count == 0)
                    return 0;

                int bytesRemaining = (this.data.Length - this.offset);
                if (count > bytesRemaining)
                {
                    count = bytesRemaining;
                }
                Buffer.BlockCopy(this.data, this.offset, buffer, offset, count);
                this.offset += count;
                return count;
            }

            public int IndexOf(byte value)
            {
                int result = -1;

                for (int i = offset; i < data.Length; i++)
                {
                    if (data[i] == value)
                    {
                        result = i;
                        break;
                    }
                }

                return result;
            }
        }

        private class DictionarySorter : IComparer<string>
        {
            public static readonly DictionarySorter instance = new DictionarySorter();

            public int Compare(string x, string y)
            {
                return string.Compare(x, y, StringComparison.Ordinal);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Decodes a BEncoded stream.
        /// </summary>
        /// <param name="data">The data to decode.</param>
        /// <returns>The decoded object.</returns>
        public static object Decode(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            var reader = new Reader(data, 0);
            return DecodeObject(reader);
        }

        /// <summary>
        /// Decodes a BEncoded stream.
        /// </summary>
        /// <param name="stream">The stream to decode.</param>
        /// <returns>The decoded object.</returns>
        public static object Decode(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            else if (!stream.CanRead)
                throw new ArgumentException("The stream cannot be read from.", "stream");

            using (var memStream = new MemoryStream())
            {
                int readBytes;
                byte[] buffer = new byte[6 * 1024];
                
                while ((readBytes = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    memStream.Write(buffer, 0, readBytes);
                }

                int streamLength = (int)memStream.Length;
                byte[] streamData = new byte[streamLength];
                memStream.Read(streamData, 0, streamLength);

                var reader = new Reader(streamData, 0);
                return DecodeObject(reader);
            }
        }

        /// <summary>
        /// Decodes a BEncoded file.
        /// </summary>
        /// <param name="filePath">The path to the file to decode.</param>
        /// <returns>The decoded object.</returns>
        public static object DecodeFromFile(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException("filePath");

            byte[] fileData = File.ReadAllBytes(filePath);
            var reader = new Reader(fileData, 0);
            return DecodeObject(reader);
        }

        /// <summary>
        /// Encodes an object using Bencoding to a stream.
        /// </summary>
        /// <param name="obj">The object to encode.</param>
        /// <returns>The encoded object.</returns>
        public static byte[] Encode(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            using (var stream = new MemoryStream())
            {
                EncodeObject(obj, stream);

                int streamLength = (int)stream.Length;
                byte[] streamData = new byte[streamLength];
                stream.Read(streamData, 0, streamLength);

                return streamData;
            }
        }

        /// <summary>
        /// Encodes an object using Bencoding to a stream.
        /// </summary>
        /// <param name="obj">The object to encode.</param>
        /// <param name="outputStream">The output stream.</param>
        public static void Encode(object obj, Stream outputStream)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            else if (outputStream == null)
                throw new ArgumentNullException("outputStream");
            else if (!outputStream.CanWrite)
                throw new ArgumentException("The output stream cannot be written to.", "outputStream");

            EncodeObject(obj, outputStream);
        }

        /// <summary>
        /// Encodes an object using Bencoding to a file.
        /// </summary>
        /// <param name="obj">The object to encode.</param>
        /// <param name="filePath">The output file path to save the encoded object.</param>
        public static void EncodeToFile(object obj, string filePath)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            else if (filePath == null)
                throw new ArgumentNullException("filePath");

            using (var stream = File.Create(filePath))
            {
                EncodeObject(obj, stream);
            }
        }
        #endregion

        #region Private Methods
        #region Decoding
        private static object DecodeObject(Reader reader)
        {
            byte nextByte = reader.Peek();
            if (nextByte == DictionaryByte)
            {
                return DecodeDictionary(reader);
            }
            else if (nextByte == ListByte)
            {
                return DecodeList(reader);
            }
            else if (nextByte == IntegerByte)
            {
                return DecodeInteger(reader);
            }
            else if (nextByte >= '0' && nextByte <= '9')
            {
                return DecodeByteArray(reader);
            }
            else
            {
                throw new InvalidDataException("The data is corrupt and does not represent valid BEncoding. Expected a new object.");
            }
        }

        private static Dictionary DecodeDictionary(Reader reader)
        {
            if (reader.Pop() != DictionaryByte)
                throw new InvalidDataException("The data is corrupt and does not represent valid BEncoding. Expected a dictionary.");

            var dictionary = new Dictionary();

            while (reader.Peek() != EndByte)
            {
                string key = DecodeString(reader);
                object value = DecodeObject(reader);
                dictionary.Add(key, value);
            }

            if (reader.Pop() != EndByte)
                throw new InvalidDataException("The data is corrupt and does not represent valid BEncoding. Expected end of dictionary.");

            // Validate to make sure that the dictionary is already sorted
            var sortedKeys = dictionary.Keys.OrderBy((x) => x, DictionarySorter.instance);
            if (!sortedKeys.SequenceEqual(dictionary.Keys))
                throw new InvalidDataException("The data is corrupt and does not represent valid BEncoding. Expected a sorted dictionary.");

            return dictionary;
        }

        private static List DecodeList(Reader reader)
        {
            if (reader.Pop() != ListByte)
                throw new InvalidDataException("The data is corrupt and does not represent valid BEncoding. Expected a list.");

            var list = new List();

            while (reader.Peek() != EndByte)
            {
                object item = DecodeObject(reader);
                list.Add(item);
            }

            if (reader.Pop() != EndByte)
                throw new InvalidDataException("The data is corrupt and does not represent valid BEncoding. Expected end of list.");

            return list;
        }

        private static long DecodeInteger(Reader reader)
        {
            if (reader.Pop() != IntegerByte)
                throw new InvalidDataException("The data is corrupt and does not represent valid BEncoding. Expected an integer.");

            int startIndex = reader.Offset;
            int endIndex = reader.IndexOf(EndByte);
            if (endIndex == -1)
                throw new InvalidDataException("The data is corrupt and does not represent valid BEncoding. Expected end of integer.");

            int integerDigitCount = (endIndex - startIndex);
            byte[] integerDigitBytes = new byte[integerDigitCount];
            integerDigitCount = reader.Read(integerDigitBytes, 0, integerDigitCount);

            long integerValue;
            string integerText = Encoding.ASCII.GetString(integerDigitBytes, 0, integerDigitCount);
            if (integerText.Length > 1 && integerText[0] == '0')
                throw new InvalidDataException("The data is corrupt and does not represent valid BEncoding. Expected integer without leading zeroes.");

            if (!long.TryParse(integerText, NumberStyles.Integer, CultureInfo.InvariantCulture, out integerValue))
                throw new InvalidDataException("The data is corrupt and does not represent valid BEncoding. Expected integer.");

            if (reader.Pop() != EndByte)
                throw new InvalidDataException("The data is corrupt and does not represent valid BEncoding. Expected end of integer.");

            return integerValue;
        }

        private static byte[] DecodeByteArray(Reader reader)
        {
            int lengthStartIndex = reader.Offset;
            int lengthEndIndex = reader.IndexOf(StringLengthDivideByte);
            if (lengthEndIndex == -1)
                throw new InvalidDataException("The data is corrupt and does not represent valid BEncoding. Expected string length divider.");

            int lengthDigitCount = (lengthEndIndex - lengthStartIndex);
            byte[] lengthDigitBytes = new byte[lengthDigitCount];
            lengthDigitCount = reader.Read(lengthDigitBytes, 0, lengthDigitCount);

            if (reader.Pop() != StringLengthDivideByte)
                throw new InvalidDataException("The data is corrupt and does not represent valid BEncoding. Expected string length divider.");

            int stringLength;
            string stringLengthText = Encoding.ASCII.GetString(lengthDigitBytes, 0, lengthDigitCount);
            if (!int.TryParse(stringLengthText, NumberStyles.Integer, CultureInfo.InvariantCulture, out stringLength) || stringLength < 0)
                throw new InvalidDataException("The data is corrupt and does not represent valid BEncoding. Expected string length integer.");

            byte[] stringBytes = new byte[stringLength];
            int readStringLength = reader.Read(stringBytes, 0, stringLength);
            if (readStringLength != stringLength)
                throw new InvalidDataException("The data is corrupt and does not represent valid BEncoding. String was shorter than expected.");

            return stringBytes;
        }

        private static string DecodeString(Reader reader)
        {
            byte[] stringBytes = DecodeByteArray(reader);
            return Encoding.UTF8.GetString(stringBytes);
        }
        #endregion

        #region Encoding
        private static void EncodeObject(object obj, Stream stream)
        {
            if (obj == null)
                throw new InvalidOperationException("Null values cannot be encoded.");

            if (obj is Dictionary<string, object>)
            {
                EncodeDictionary(obj as Dictionary<string, object>, stream);
            }
            else if (obj is List<object>)
            {
                EncodeList(obj as List<object>, stream);
            }
            else if (obj is long)
            {
                EncodeInteger((long)obj, stream);
            }
            else if (obj is int)
            {
                EncodeInteger((int)obj, stream);
            }
            else if (obj is byte[])
            {
                EncodeByteArray(obj as byte[], stream);
            }
            else if (obj is string)
            {
                EncodeString(obj as string, stream);
            }
            else if (obj is Array)
            {
                EncodeArray(obj as Array, stream);
            }
            else
            {
                throw new InvalidOperationException(string.Format("There is no support to encode the type: {0}", obj.GetType().FullName));
            }
        }

        private static void EncodeDictionary(Dictionary<string, object> dictionary, Stream stream)
        {
            stream.WriteByte(DictionaryByte);

            var sortedDictionary = new SortedDictionary<string, object>(dictionary, DictionarySorter.instance);
            foreach (var item in sortedDictionary)
            {
                EncodeString(item.Key, stream);
                EncodeObject(item.Value, stream);
            }

            stream.WriteByte(EndByte);
        }

        private static void EncodeList(List<object> list, Stream stream)
        {
            stream.WriteByte(ListByte);

            foreach (var item in list)
            {
                EncodeObject(item, stream);
            }

            stream.WriteByte(EndByte);
        }

        private static void EncodeArray(Array array, Stream stream)
        {
            stream.WriteByte(ListByte);

            int arrayLength = array.Length;
            for (int i = 0; i < arrayLength; i++)
            {
                object item = array.GetValue(i);
                EncodeObject(item, stream);
            }

            stream.WriteByte(EndByte);
        }

        private static void EncodeInteger(long value, Stream stream)
        {
            stream.WriteByte(IntegerByte);

            string valueText = value.ToString(CultureInfo.InvariantCulture);
            byte[] valueDigitBytes = Encoding.ASCII.GetBytes(valueText);
            stream.Write(valueDigitBytes, 0, valueDigitBytes.Length);

            stream.WriteByte(EndByte);
        }

        private static void EncodeByteArray(byte[] data, Stream stream)
        {
            string stringLengthText = data.Length.ToString(CultureInfo.InvariantCulture);
            byte[] lengthDigitBytes = Encoding.ASCII.GetBytes(stringLengthText);

            stream.Write(lengthDigitBytes, 0, lengthDigitBytes.Length);
            stream.WriteByte(StringLengthDivideByte);
            stream.Write(data, 0, data.Length);
        }

        private static void EncodeString(string text, Stream stream)
        {
            byte[] stringBytes = Encoding.UTF8.GetBytes(text);
            EncodeByteArray(stringBytes, stream);
        }
        #endregion
        #endregion
    }
}
