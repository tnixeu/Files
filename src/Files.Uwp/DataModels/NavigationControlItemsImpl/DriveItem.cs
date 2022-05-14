using Files.Uwp.Extensions;
using Files.Uwp.Helpers;
using Files.Shared.Extensions;
using Microsoft.Toolkit.Uwp;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Files.Backend.Models;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml;
using Windows.ApplicationModel.Core;
using Files.Backend.DataModels.NavigationControlItems;

namespace Files.Uwp.DataModels.NavigationControlItems
{
    public class DriveItem : DriveItemBase
    {
        private BitmapImage icon;
        public BitmapImage Icon
        {
            get => icon;
            set => SetProperty(ref icon, value);
        }

        public StorageFolder Root { get; set; }

        public Visibility ItemVisibility { get; set; } = Visibility.Visible;
        public Visibility ShowDriveDetails
        {
            get => MaxSpace.Bytes > 0d ? Visibility.Visible : Visibility.Collapsed;
        }

        private float percentageUsed = 0.0f;
        public float PercentageUsed
        {
            get => percentageUsed;
            set
            {
                if (SetProperty(ref percentageUsed, value))
                {
                    if (Type == DriveType.Fixed)
                    {
                        if (percentageUsed >= Constants.Widgets.Drives.LowStorageSpacePercentageThreshold)
                        {
                            ShowStorageSense = true;
                        }
                        else
                        {
                            ShowStorageSense = false;
                        }
                    }
                }
            }
        }

        public static async Task<DriveItem> CreateFromPropertiesAsync(StorageFolder root, string deviceId, DriveType type, IRandomAccessStream imageStream = null)
        {
            var item = new DriveItem();

            if (imageStream != null)
            {
                item.IconData = await imageStream.ToByteArrayAsync();
            }

            item.Text = root.DisplayName;
            item.Type = type;
            item.MenuOptions = new ContextMenuOptions
            {
                IsLocationItem = true,
                ShowEjectDevice = item.IsRemovable,
                ShowShellItems = true,
                ShowProperties = true
            };
            item.Path = string.IsNullOrEmpty(root.Path) ? $"\\\\?\\{root.Name}\\" : root.Path;
            item.DeviceID = deviceId;
            item.Root = root;

            _ = CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => item.UpdatePropertiesAsync());

            return item;
        }

        public async Task UpdateLabelAsync()
        {
            try
            {
                var properties = await Root.Properties.RetrievePropertiesAsync(new[] { "System.ItemNameDisplay" })
                    .AsTask().WithTimeoutAsync(TimeSpan.FromSeconds(5));
                Text = (string)properties["System.ItemNameDisplay"];
            }
            catch (NullReferenceException)
            {
            }
        }

        public async Task UpdatePropertiesAsync()
        {
            try
            {
                var properties = await Root.Properties.RetrievePropertiesAsync(new[] { "System.FreeSpace", "System.Capacity" })
                    .AsTask().WithTimeoutAsync(TimeSpan.FromSeconds(5));

                if (properties != null && properties["System.Capacity"] != null && properties["System.FreeSpace"] != null)
                {
                    MaxSpace = ByteSizeLib.ByteSize.FromBytes((ulong)properties["System.Capacity"]);
                    FreeSpace = ByteSizeLib.ByteSize.FromBytes((ulong)properties["System.FreeSpace"]);
                    SpaceUsed = MaxSpace - FreeSpace;

                    SpaceText = string.Format(
                        "DriveFreeSpaceAndCapacity".GetLocalized(),
                        FreeSpace.ToSizeString(),
                        MaxSpace.ToSizeString());

                    if (FreeSpace.Bytes > 0 && MaxSpace.Bytes > 0) // Make sure we don't divide by 0
                    {
                        PercentageUsed = 100.0f - ((float)(FreeSpace.Bytes / MaxSpace.Bytes) * 100.0f);
                    }
                }
                else
                {
                    SpaceText = "DriveCapacityUnknown".GetLocalized();
                    MaxSpace = SpaceUsed = FreeSpace = ByteSizeLib.ByteSize.FromBytes(0);
                }
            }
            catch (Exception)
            {
                SpaceText = "DriveCapacityUnknown".GetLocalized();
                MaxSpace = SpaceUsed = FreeSpace = ByteSizeLib.ByteSize.FromBytes(0);
            }
        }

        public async Task LoadDriveIcon()
        {
            if (IconData == null)
            {
                if (!string.IsNullOrEmpty(DeviceID))
                {
                    IconData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(DeviceID, 24);
                }
                if (IconData == null)
                {
                    var resource = await UIHelpers.GetIconResourceInfo(Constants.ImageRes.Folder);
                    IconData = resource?.IconDataBytes;
                }
            }
            Icon = await IconData.ToBitmapAsync();
        }
    }
}