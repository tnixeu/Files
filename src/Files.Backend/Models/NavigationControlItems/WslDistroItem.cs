using Files.Backend.Enums;
using Files.Backend.Models;
using System;

namespace Files.Backend.DataModels.NavigationControlItems
{
    public class WslDistroItem : INavigationControlItem
    {
        public string Text { get; set; }

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

        public NavigationControlItemType ItemType => NavigationControlItemType.LinuxDistro;

        public Uri Logo { get; set; }

        public SectionType Section { get; set; }

        public ContextMenuOptions MenuOptions { get; set; }

        public int CompareTo(INavigationControlItem other) => Text.CompareTo(other.Text);
    }
}