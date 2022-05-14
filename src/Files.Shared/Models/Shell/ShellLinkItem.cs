namespace Files.Shared.Models.Shell
{
    public class ShellLinkItem : ShellFileItem
    {
        public string TargetPath;
        public string Arguments;
        public string WorkingDirectory;
        public bool RunAsAdmin;

        public ShellLinkItem()
        {
        }

        public ShellLinkItem(ShellFileItem baseItem)
        {
            RecyclePath = baseItem.RecyclePath;
            FileName = baseItem.FileName;
            FilePath = baseItem.FilePath;
            RecycleDate = baseItem.RecycleDate;
            ModifiedDate = baseItem.ModifiedDate;
            CreatedDate = baseItem.CreatedDate;
            FileSize = baseItem.FileSize;
            FileSizeBytes = baseItem.FileSizeBytes;
            FileType = baseItem.FileType;
        }
    }
}
