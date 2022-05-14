using CommunityToolkit.Mvvm.ComponentModel;
using Files.Backend.Enums;
using Files.Backend.Models;
using ByteSize = ByteSizeLib.ByteSize;

namespace Files.Backend.DataModels.NavigationControlItems
{
    public abstract class DriveItemBase : ObservableObject, INavigationControlItem
    {
        public byte[] IconData { get; set; }

        private string path;

        public string Path
        {
            get => path;
            set
            {
                path = value;
                HoverDisplayText = Path.Contains("?") ? Text : Path;
            }
        }

        public string HoverDisplayText { get; private set; }
        public string DeviceID { get; set; }
        public NavigationControlItemType ItemType { get; set; } = NavigationControlItemType.Drive;

        public bool IsRemovable => Type == DriveType.Removable || Type == DriveType.CDRom;
        public bool IsNetwork => Type == DriveType.Network;

        private ByteSize maxSpace;
        private ByteSize freeSpace;
        private ByteSize spaceUsed;

        public ByteSize MaxSpace
        {
            get => maxSpace;
            set => SetProperty(ref maxSpace, value);
        }

        public ByteSize FreeSpace
        {
            get => freeSpace;
            set => SetProperty(ref freeSpace, value);
        }

        public ByteSize SpaceUsed
        {
            get => spaceUsed;
            set => SetProperty(ref spaceUsed, value);
        }

        private DriveType type;

        public DriveType Type
        {
            get => type;
            set
            {
                type = value;
            }
        }

        private string text;

        public string Text
        {
            get => text;
            set => SetProperty(ref text, value);
        }

        private string spaceText;

        public string SpaceText
        {
            get => spaceText;
            set => SetProperty(ref spaceText, value);
        }

        public SectionType Section { get; set; }

        public ContextMenuOptions MenuOptions { get; set; }

        private bool showStorageSense = false;

        public bool ShowStorageSense
        {
            get => showStorageSense;
            set => SetProperty(ref showStorageSense, value);
        }

        public DriveItemBase()
        {
            ItemType = NavigationControlItemType.CloudDrive;
        }

        public int CompareTo(INavigationControlItem other)
        {
            var result = Type.CompareTo((other as DriveItemBase)?.Type ?? Type);
            if (result == 0)
            {
                return Text.CompareTo(other.Text);
            }
            return result;
        }
    }

    public enum DriveType
    {
        Fixed,
        Removable,
        Network,
        Ram,
        CDRom,
        FloppyDisk,
        Unknown,
        NoRootDirectory,
        VirtualDrive,
        CloudDrive,
    }
}