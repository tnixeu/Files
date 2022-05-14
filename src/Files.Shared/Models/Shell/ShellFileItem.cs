using System;

namespace Files.Shared.Models.Shell
{
    public class ShellFileItem
    {
        public bool IsFolder;
        public string RecyclePath;
        public string FileName;
        public string FilePath;
        public DateTime RecycleDate;
        public DateTime ModifiedDate;
        public DateTime CreatedDate;
        public string FileSize;
        public ulong FileSizeBytes;
        public string FileType;

        public ShellFileItem()
        {
        }

        public ShellFileItem(
            bool isFolder, string recyclePath, string fileName, string filePath,
            DateTime recycleDate, DateTime modifiedDate, DateTime createdDate, string fileSize, ulong fileSizeBytes, string fileType)
        {
            IsFolder = isFolder;
            RecyclePath = recyclePath;
            FileName = fileName;
            FilePath = filePath;
            RecycleDate = recycleDate;
            ModifiedDate = modifiedDate;
            CreatedDate = createdDate;
            FileSize = fileSize;
            FileSizeBytes = fileSizeBytes;
            FileType = fileType;
        }
    }
}