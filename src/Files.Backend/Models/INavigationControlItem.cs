using Files.Backend.Enums;
using System;

namespace Files.Backend.Models
{
    public interface INavigationControlItem : IComparable<INavigationControlItem>
    {
        string Text { get; }

        string Path { get; }

        SectionType Section { get; }

        string HoverDisplayText { get; }

        NavigationControlItemType ItemType { get; }

        ContextMenuOptions MenuOptions { get; }
    }

    public class ContextMenuOptions
    {
        public bool IsLibrariesHeader { get; set; }

        public bool ShowHideSection { get; set; }

        public bool IsLocationItem { get; set; }

        public bool ShowUnpinItem { get; set; }

        public bool IsItemMovable { get; set; }

        public bool ShowProperties { get; set; }

        public bool ShowEmptyRecycleBin { get; set; }

        public bool ShowEjectDevice { get; set; }

        public bool ShowShellItems { get; set; }
    }
}