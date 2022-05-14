using Files.Backend.DataModels.NavigationControlItems;
using Files.Backend.Enums;
using Files.Backend.Models;
using Files.Backend.Services;
using Files.Shared.Extensions;
using Files.Uwp.DataModels.NavigationControlItems;
using Files.Uwp.Filesystem;
using Files.Uwp.Helpers;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace Files.Uwp.ServicesImplementation
{
    public class PinnedItemsService : IPinnedItemsService
    {
        private StorageItemAccessList futureAccessList = StorageApplicationPermissions.FutureAccessList;

        public async Task<IList<LocationItemBase>> GetPinnedItemsAsync()
        {
            List<LocationItemBase> favoriteItems = new List<LocationItemBase>();

            if (!futureAccessList.Entries.Any())
            {
                foreach (string path in GetDefaultItems())
                {
                    await AddPinnedItemByPathAsync(path);
                }

                if (ReadV2PinnedItemsFile() is IEnumerable<string> paths)
                {
                    foreach (string path in paths)
                    {
                        await AddPinnedItemByPathAsync(path);
                    }
                }
            }

            foreach (AccessListEntry item in futureAccessList.Entries)
            {
                StorageFolder folder = await futureAccessList.GetFolderAsync(
                    item.Token,
                    AccessCacheOptions.SuppressAccessTimeUpdate |
                    AccessCacheOptions.UseReadOnlyCachedCopy |
                    AccessCacheOptions.FastLocationsOnly |
                    AccessCacheOptions.DisallowUserInput);

                var sidebarItem = await GetSidebarItemFromPathAsync(folder.Path, folder);

                favoriteItems.AddIfNotPresent(sidebarItem);
            }

            return favoriteItems;
        }

        public async Task<List<string>> GetPinnedItemPathsAsync()
        {
            List<string> pinnedItemPaths = new List<string>();

            foreach (AccessListEntry item in futureAccessList.Entries)
            {
                StorageFolder folder = await futureAccessList.GetFolderAsync(
                    item.Token,
                    AccessCacheOptions.SuppressAccessTimeUpdate |
                    AccessCacheOptions.UseReadOnlyCachedCopy |
                    AccessCacheOptions.FastLocationsOnly |
                    AccessCacheOptions.DisallowUserInput);

                pinnedItemPaths.AddIfNotPresent(folder.Path);
            }

            return pinnedItemPaths;
        }

        public async Task AddPinnedItemAsync(LocationItemBase item, int index = -1)
        {
            if (!await CheckPinnedStatusByPathAsync(item.Path))
            {
                var folder = await StorageFolder.GetFolderFromPathAsync(item.Path);

                if (index > -1)
                {
                    futureAccessList.Add(folder, index.ToString());
                }
                else
                {
                    futureAccessList.Add(folder);
                }
            }
        }

        public async Task AddPinnedItemByPathAsync(string path, int index = -1)
        {
            if (!await CheckPinnedStatusByPathAsync(path))
            {
                var folder = await StorageFolder.GetFolderFromPathAsync(path);

                if (index > -1)
                {
                    futureAccessList.Add(folder, index.ToString());
                }
                else
                {
                    futureAccessList.Add(folder);
                }
            }
        }

        public async Task AddPinnedItemsByPathAsync(IList<string> paths)
        {
            foreach (string path in paths)
            {
                if (!await CheckPinnedStatusByPathAsync(path))
                {
                    var folder = await StorageFolder.GetFolderFromPathAsync(path);
                    futureAccessList.Add(folder);
                }
            }
        }

        public async Task<bool> CheckPinnedStatusByPathAsync(string path)
        {
            if (await GetEntryTokenFromPathAsync(path) is string token)
            {
                return futureAccessList.ContainsItem(token);
            }
            else
            {
                return false;
            }
        }

        public async Task RemovePinnedItemAsync(LocationItemBase item)
        {
            var token = await GetEntryTokenFromPathAsync(item.Path);
            futureAccessList.Remove(token);
        }

        public async Task RemovePinnedItemByPathAsync(string path)
        {
            var token = await GetEntryTokenFromPathAsync(path);
            futureAccessList.Remove(token);
        }

        public async Task RemovePinnedItemsByPathAsync(IList<string> paths)
        {
            foreach (string path in paths)
            {
                await RemovePinnedItemByPathAsync(path);
            }
        }

        public void RefreshPinnedItems()
        {
            throw new NotImplementedException();
        }

        public void RemovePinnedItemAt(int index)
        {
            string token = GetEntryTokenWithIndex(index);
            if (string.IsNullOrWhiteSpace(token))
            {
                futureAccessList.Remove(token);
            }
        }

        public async Task<LocationItem> GetSidebarItemFromPathAsync(string path, StorageFolder folder = null)
        {
            var item = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(path));
            folder ??= await (await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path, item))).Result.ToStorageFolderAsync();
            var locationItem = new LocationItem
            {
                Font = App.MainViewModel.FontName,
                Path = path,
                Section = SectionType.Favorites,
                MenuOptions = new ContextMenuOptions
                {
                    IsLocationItem = true,
                    ShowProperties = true,
                    ShowUnpinItem = true,
                    ShowShellItems = true,
                    IsItemMovable = true
                },
                IsDefaultLocation = false,
                Text = folder?.DisplayName ?? Path.GetFileName(path.TrimEnd('\\'))
            };

            if (folder is StorageFolder || (FilesystemResult)FolderHelpers.CheckFolderAccessWithWin32(path))
            {
                locationItem.IsInvalid = false;
                var iconData = await FileThumbnailHelper.LoadIconFromStorageItemAsync(folder, 24u, Windows.Storage.FileProperties.ThumbnailMode.ListView);

                if (iconData == null)
                {
                    iconData = await FileThumbnailHelper.LoadIconFromStorageItemAsync(folder, 24u, Windows.Storage.FileProperties.ThumbnailMode.SingleItem);
                }

                locationItem.IconData = iconData;
                locationItem.Icon = await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => locationItem.IconData.ToBitmapAsync());
                if (locationItem.IconData == null)
                {
                    locationItem.IconData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(path, 24u);
                    if (locationItem.IconData != null)
                    {
                        locationItem.Icon = await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => locationItem.IconData.ToBitmapAsync());
                    }
                }
            }
            else
            {
                locationItem.Icon = await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => UIHelpers.GetIconResource(Constants.ImageRes.Folder));
                locationItem.IsInvalid = true;
                Debug.WriteLine($"Pinned item {path} was invalid");
            }

            return locationItem;
        }

        private async Task<string> GetEntryTokenFromPathAsync(string path)
        {
            foreach (AccessListEntry entry in futureAccessList.Entries)
            {
                StorageFolder folder = await futureAccessList.GetFolderAsync(
                    entry.Token,
                    AccessCacheOptions.SuppressAccessTimeUpdate |
                    AccessCacheOptions.UseReadOnlyCachedCopy |
                    AccessCacheOptions.FastLocationsOnly |
                    AccessCacheOptions.DisallowUserInput);

                if (Path.GetFullPath(folder.Path).Equals(Path.GetFullPath(path)))
                {
                    return entry.Token;
                }
            }

            return null;
        }

        private string GetEntryTokenWithIndex(int index)
        {
            foreach (AccessListEntry entry in futureAccessList.Entries)
            {
                if (index == Convert.ToInt32(entry.Metadata))
                {
                    return entry.Token;
                }
            }

            return null;
        }

        private List<string> GetDefaultItems()
        {
            var udp = UserDataPaths.GetDefault();
            List<string> paths = new List<string>()
            {
                CommonPaths.DesktopPath,
                CommonPaths.DownloadsPath,
                udp.Documents
            };

            return paths;
        }

        private async Task<IEnumerable<string>> ReadV2PinnedItemsFile()
        {
            return await SafetyExtensions.IgnoreExceptions(async () =>
            {
                var oldPinnedItemsFile = await ApplicationData.Current.LocalCacheFolder.GetFileAsync("PinnedItems.json");
                var model = JsonConvert.DeserializeObject<SidebarPinnedModel>(await FileIO.ReadTextAsync(oldPinnedItemsFile));
                await oldPinnedItemsFile.DeleteAsync();
                return model.FavoriteItems;
            });
        }
    }

    public class SidebarPinnedModel
    {
        [JsonProperty("items")]
        public List<string> FavoriteItems { get; set; } = new List<string>();
    }
}
