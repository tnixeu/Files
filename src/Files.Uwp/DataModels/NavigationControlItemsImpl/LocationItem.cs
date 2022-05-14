using System;
using Files.Shared.Models.Shell;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media;
using Microsoft.Toolkit.Uwp;
using Files.Backend.DataModels.NavigationControlItems;

namespace Files.Uwp.DataModels.NavigationControlItems
{
    public class LocationItem : LocationItemBase
    {
        public BitmapImage icon;

        public BitmapImage Icon
        {
            get => icon;
            set => SetProperty(ref icon, value);
        }

        public string Path
        {
            get => base.Path;
            set
            {
                base.Path = value;
                HoverDisplayText = string.IsNullOrEmpty(Path) || Path.Contains("?") || Path.StartsWith("shell:", StringComparison.OrdinalIgnoreCase) || Path.EndsWith(ShellLibraryItem.EXTENSION, StringComparison.OrdinalIgnoreCase) || Path == "Home".GetLocalized() ? Text : Path;
            }
        }

        public FontFamily Font { get; set; }
    }
}