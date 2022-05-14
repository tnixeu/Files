using CommunityToolkit.Mvvm.ComponentModel;
using Files.Backend.Models;
using Files.Backend.Enums;
using Files.Backend.Helpers;

namespace Files.Backend.DataModels.NavigationControlItems
{
    public abstract class LocationItemBase : ObservableObject, INavigationControlItem
    {
        public byte[] IconData { get; set; }
        public string Text { get; set; }
        public string Path { get; set; }
        public string HoverDisplayText { get; set; }
        public NavigationControlItemType ItemType => NavigationControlItemType.Location;
        public bool IsDefaultLocation { get; set; }
        public BulkConcurrentObservableCollection<INavigationControlItem> ChildItems { get; set; }
        public bool SelectsOnInvoked { get; set; } = true;
        private bool isExpanded;
        public bool IsExpanded
        {
            get => isExpanded;
            set => SetProperty(ref isExpanded, value);
        }
        public bool IsInvalid { get; set; } = false;
        public SectionType Section { get; set; }
        public ContextMenuOptions MenuOptions { get; set; }
        public int CompareTo(INavigationControlItem other) => Text.CompareTo(other.Text);
    }
}