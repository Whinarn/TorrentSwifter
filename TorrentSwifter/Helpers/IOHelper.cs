using System;
using System.IO;

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
