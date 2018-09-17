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
using TorrentSwifter.Torrents;

namespace TorrentSwifter.Helpers
{
    internal static class IOHelper
    {
        public static string GetParentDirectory(string filePath)
        {
            return Path.GetDirectoryName(filePath);
        }

        public static void CreateDirectoryIfItDoesntExist(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        public static void CreateParentDirectoryIfItDoesntExist(string filePath)
        {
            string parentDirectoryPath = GetParentDirectory(filePath);
            CreateDirectoryIfItDoesntExist(parentDirectoryPath);
        }

        public static void CreateEmptyFile(string filePath)
        {
            if (File.Exists(filePath))
                return;

            CreateParentDirectoryIfItDoesntExist(filePath);

            var fileStream = File.Create(filePath);
            fileStream.Dispose();
        }

        public static void CreateAllocatedFile(string filePath, long fileSize)
        {
            if (File.Exists(filePath))
                return;

            CreateParentDirectoryIfItDoesntExist(filePath);

            using (var fileStream = File.Create(filePath))
            {
                fileStream.SetLength(fileSize);
            }
        }

        public static string GetLocalPath(string fullPath, string rootPath)
        {
            fullPath = Path.GetFullPath(fullPath);
            rootPath = Path.GetFullPath(rootPath);

            if (Path.DirectorySeparatorChar != '/')
            {
                fullPath = fullPath.Replace(Path.DirectorySeparatorChar, '/');
                rootPath = rootPath.Replace(Path.DirectorySeparatorChar, '/');
            }
            if (Path.AltDirectorySeparatorChar != '/')
            {
                fullPath = fullPath.Replace(Path.AltDirectorySeparatorChar, '/');
                rootPath = rootPath.Replace(Path.AltDirectorySeparatorChar, '/');
            }

            // Add the ending slash if it's not present
            if (!rootPath.EndsWith("/"))
            {
                rootPath += "/";
            }

            if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(string.Format("The path '{0}' is not a part of the directory '{1}'.", fullPath, rootPath));

            string localPath = fullPath.Substring(rootPath.Length);
            return localPath;
        }

        public static string GetTorrentFilePath(string rootPath, TorrentMetaData.FileItem fileItem)
        {
            string fileLocalPath = fileItem.Path;
            if (fileLocalPath == null)
                throw new InvalidOperationException("The file does not have a path.");

            if (Path.DirectorySeparatorChar != '/')
            {
                fileLocalPath = fileLocalPath.Replace('/', Path.DirectorySeparatorChar);
            }

            string fileFullPath = Path.Combine(rootPath, fileLocalPath);
            fileFullPath = Path.GetFullPath(fileFullPath);
            return fileFullPath;
        }
    }
}
