using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Files.Backend.DataModels.NavigationControlItems;
using Files.Backend.Enums;
using Files.Backend.Helpers;
using Files.Backend.Models;
using Files.Backend.Services;
using Files.Backend.Services.Settings;
using Files.Shared.EventArguments;
using Files.Shared.Extensions;
using Files.Uwp.DataModels.NavigationControlItems;
using Files.Uwp.Filesystem;
using Files.Uwp.Helpers;
using Files.Uwp.UserControls;
using Microsoft.Toolkit.Uwp;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Uwp.ViewModels
{
    public class SidebarViewModel : ObservableObject, IDisposable
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();
        private IPinnedItemsService pinnedItemsService = Ioc.Default.GetService<IPinnedItemsService>();

        public ICommand EmptyRecycleBinCommand { get; private set; }
        private SemaphoreSlim addSyncSemaphore;

        private IPaneHolder paneHolder;

        public IPaneHolder PaneHolder
        {
            get => paneHolder;
            set => SetProperty(ref paneHolder, value);
        }

        public IFilesystemHelpers FilesystemHelpers => PaneHolder?.FilesystemHelpers;

        private DispatcherQueue dispatcherQueue;

        public BulkConcurrentObservableCollection<INavigationControlItem> SideBarItems { get; init; }

        public static readonly GridLength CompactSidebarWidth = SidebarControl.GetSidebarCompactSize();

        private NavigationViewDisplayMode sidebarDisplayMode;

        public NavigationViewDisplayMode SidebarDisplayMode
        {
            get => sidebarDisplayMode;
            set
            {
                if (SetProperty(ref sidebarDisplayMode, value))
                {
                    OnPropertyChanged(nameof(IsSidebarCompactSize));
                    UpdateTabControlMargin();
                }
            }
        }

        public IReadOnlyList<INavigationControlItem> Favorites
        {
            get
            {
                lock (favoriteList)
                {
                    return favoriteList.ToList().AsReadOnly();
                }
            }
        }
        private List<INavigationControlItem> favoriteList = new List<INavigationControlItem>();

        public bool IsSidebarCompactSize => SidebarDisplayMode == NavigationViewDisplayMode.Compact || SidebarDisplayMode == NavigationViewDisplayMode.Minimal;

        public void NotifyInstanceRelatedPropertiesChanged(string arg)
        {
            UpdateSidebarSelectedItemFromArgs(arg);

            OnPropertyChanged(nameof(SidebarSelectedItem));
        }

        public void UpdateSidebarSelectedItemFromArgs(string arg)
        {
            var value = arg;

            INavigationControlItem item = null;
            List<INavigationControlItem> sidebarItems = SideBarItems
                .Where(x => !string.IsNullOrWhiteSpace(x.Path))
                .Concat(SideBarItems.Where(x => (x as LocationItem)?.ChildItems != null).SelectMany(x => (x as LocationItem).ChildItems).Where(x => !string.IsNullOrWhiteSpace(x.Path)))
                .ToList();

            if (string.IsNullOrEmpty(value))
            {
                //SidebarSelectedItem = sidebarItems.FirstOrDefault(x => x.Path.Equals("Home".GetLocalized()));
                return;
            }

            item = sidebarItems.FirstOrDefault(x => x.Path.Equals(value, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                item = sidebarItems.FirstOrDefault(x => x.Path.Equals(value + "\\", StringComparison.OrdinalIgnoreCase));
            }
            if (item == null)
            {
                item = sidebarItems.FirstOrDefault(x => value.StartsWith(x.Path, StringComparison.OrdinalIgnoreCase));
            }
            if (item == null)
            {
                item = sidebarItems.FirstOrDefault(x => x.Path.Equals(Path.GetPathRoot(value), StringComparison.OrdinalIgnoreCase));
            }
            if (item == null)
            {
                if (value == "Home".GetLocalized())
                {
                    item = sidebarItems.FirstOrDefault(x => x.Path.Equals("Home".GetLocalized()));
                }
            }

            if (SidebarSelectedItem != item)
            {
                SidebarSelectedItem = item;
            }
        }

        public bool IsSidebarOpen
        {
            get => UserSettingsService.AppearanceSettingsService.IsSidebarOpen;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.IsSidebarOpen)
                {
                    UserSettingsService.AppearanceSettingsService.IsSidebarOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowFavoritesSection
        {
            get => UserSettingsService.AppearanceSettingsService.ShowFavoritesSection;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.ShowFavoritesSection)
                {
                    UserSettingsService.AppearanceSettingsService.ShowFavoritesSection = value;
                }
            }
        }

        public bool ShowLibrarySection
        {
            get => UserSettingsService.AppearanceSettingsService.ShowLibrarySection;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.ShowLibrarySection)
                {
                    UserSettingsService.AppearanceSettingsService.ShowLibrarySection = value;
                }
            }
        }

        public bool ShowDrivesSection
        {
            get => UserSettingsService.AppearanceSettingsService.ShowDrivesSection;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.ShowDrivesSection)
                {
                    UserSettingsService.AppearanceSettingsService.ShowDrivesSection = value;
                }
            }
        }

        public bool ShowCloudDrivesSection
        {
            get => UserSettingsService.AppearanceSettingsService.ShowCloudDrivesSection;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.ShowCloudDrivesSection)
                {
                    UserSettingsService.AppearanceSettingsService.ShowCloudDrivesSection = value;
                }
            }
        }

        public bool ShowNetworkDrivesSection
        {
            get => UserSettingsService.AppearanceSettingsService.ShowNetworkDrivesSection;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.ShowNetworkDrivesSection)
                {
                    UserSettingsService.AppearanceSettingsService.ShowNetworkDrivesSection = value;
                }
            }
        }

        public bool ShowWslSection
        {
            get => UserSettingsService.AppearanceSettingsService.ShowWslSection;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.ShowWslSection)
                {
                    UserSettingsService.AppearanceSettingsService.ShowWslSection = value;
                }
            }
        }

        public bool ShowFileTagsSection
        {
            get => UserSettingsService.AppearanceSettingsService.ShowFileTagsSection;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.ShowFileTagsSection)
                {
                    UserSettingsService.AppearanceSettingsService.ShowFileTagsSection = value;
                }
            }
        }

        public bool AreFileTagsEnabled
        {
            get => UserSettingsService.PreferencesSettingsService.AreFileTagsEnabled;
        }

        private INavigationControlItem selectedSidebarItem;

        public INavigationControlItem SidebarSelectedItem
        {
            get => selectedSidebarItem;
            set => SetProperty(ref selectedSidebarItem, value);
        }

        public SidebarViewModel()
        {
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            addSyncSemaphore = new SemaphoreSlim(1, 1);

            SideBarItems = new BulkConcurrentObservableCollection<INavigationControlItem>();
            EmptyRecycleBinCommand = new RelayCommand<RoutedEventArgs>(EmptyRecycleBin);
            UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;

            InitializeData();

            App.LibraryManager.DataChanged += async (x, y) => await DataChangedAsync((SectionType)x, y);
            App.DrivesManager.DataChanged += async (x, y) => await DataChangedAsync((SectionType)x, y);
            App.CloudDrivesManager.DataChanged += async (x, y) => await DataChangedAsync((SectionType)x, y);
            App.NetworkDrivesManager.DataChanged += async (x, y) => await DataChangedAsync((SectionType)x, y);
            App.WSLDistroManager.DataChanged += async (x, y) => await DataChangedAsync((SectionType)x, y);
            App.FileTagsManager.DataChanged += async (x, y) => await DataChangedAsync((SectionType)x, y);
        }

        private async void InitializeData()
        {
            await AddPinnedItemsToSidebarAsync();
            //await DataChangedAsync(SectionType.Favorites, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            await DataChangedAsync(SectionType.Library, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            await DataChangedAsync(SectionType.Drives, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            await DataChangedAsync(SectionType.CloudDrives, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            await DataChangedAsync(SectionType.Network, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            await DataChangedAsync(SectionType.WSL, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            await DataChangedAsync(SectionType.FileTag, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private async Task DataChangedAsync(SectionType sender, NotifyCollectionChangedEventArgs e)
        {
            await dispatcherQueue.EnqueueAsync(async () =>
            {
                var section = await GetOrCreateSectionAsync(sender);
                Func<IReadOnlyList<INavigationControlItem>> getElements = () => sender switch
                {
                    SectionType.Favorites => favoriteList,
                    SectionType.CloudDrives => App.CloudDrivesManager.Drives.Cast<INavigationControlItem>().ToList(),
                    SectionType.Drives => App.DrivesManager.Drives.Cast<INavigationControlItem>().ToList(),
                    SectionType.Network => App.NetworkDrivesManager.Drives.Cast<INavigationControlItem>().ToList(),
                    SectionType.WSL => App.WSLDistroManager.Distros.Cast<INavigationControlItem>().ToList(),
                    SectionType.Library => App.LibraryManager.Libraries.Cast<INavigationControlItem>().ToList(),
                    SectionType.FileTag => App.FileTagsManager.FileTags.Cast<INavigationControlItem>().ToList(),
                    _ => null
                };
                await SyncSidebarItems(section, getElements, e);
            });
        }

        private async Task SyncSidebarItems(LocationItem section, Func<IReadOnlyList<INavigationControlItem>> getElements, NotifyCollectionChangedEventArgs e)
        {
            if (section == null)
            {
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        for (int i = 0; i < e.NewItems.Count; i++)
                        {
                            var index = e.NewStartingIndex < 0 ? -1 : i + e.NewStartingIndex;
                            await AddElementToSection((INavigationControlItem)e.NewItems[i], section, index);
                        }
                        break;
                    }
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    {
                        foreach (INavigationControlItem elem in e.OldItems)
                        {
                            var match = section.ChildItems.FirstOrDefault(x => x.Path == elem.Path);
                            section.ChildItems.Remove(match);
                        }
                        if (e.Action != NotifyCollectionChangedAction.Remove)
                        {
                            goto case NotifyCollectionChangedAction.Add;
                        }
                        break;
                    }
                case NotifyCollectionChangedAction.Reset:
                    {
                        foreach (INavigationControlItem elem in getElements())
                        {
                            await AddElementToSection(elem, section);
                        }
                        foreach (INavigationControlItem elem in section.ChildItems.ToList())
                        {
                            if (!getElements().Any(x => x.Path == elem.Path))
                            {
                                section.ChildItems.Remove(elem);
                            }
                        }
                        break;
                    }
            }
        }

        private bool IsLibraryOnSidebar(LibraryLocationItem item) => item != null && !item.IsEmpty && item.IsDefaultLocation;

        private async Task AddElementToSection(INavigationControlItem elem, LocationItem section, int index = -1)
        {
            if (elem is LibraryLocationItem lib)
            {
                if (IsLibraryOnSidebar(lib) && await lib.CheckDefaultSaveFolderAccess())
                {
                    if (!section.ChildItems.Any(x => x.Path == lib.Path))
                    {
                        lib.Font = App.MainViewModel.FontName;
                        section.ChildItems.AddSorted(elem);
                        await lib.LoadLibraryIcon();
                    }
                }
            }
            else if (elem is DriveItem drive)
            {
                if (!section.ChildItems.Any(x => x.Path == drive.Path))
                {
                    section.ChildItems.Insert(index < 0 ? section.ChildItems.Count : Math.Min(index, section.ChildItems.Count), drive);
                    await drive.LoadDriveIcon();
                }
            }
            else
            {
                if (!section.ChildItems.Any(x => x.Path == elem.Path))
                {
                    section.ChildItems.Insert(index < 0 ? section.ChildItems.Count : Math.Min(index, section.ChildItems.Count), elem);
                }
            }

            if (IsSidebarOpen)
            {
                // Restore expanded state when section has items
                section.IsExpanded = App.AppSettings.Get(section.Text == "SidebarFavorites".GetLocalized(), $"section:{section.Text.Replace('\\', '_')}");
            }
        }

        private async Task<LocationItem> GetOrCreateSectionAsync(SectionType sectionType)
        {
            var sectionOrder = new[] { SectionType.Favorites, SectionType.Library, SectionType.Drives, SectionType.CloudDrives, SectionType.Network, SectionType.WSL, SectionType.FileTag };
            switch (sectionType)
            {
                case SectionType.Favorites:
                    {
                        var section = SideBarItems.FirstOrDefault(x => x.Text == "SidebarFavorites".GetLocalized()) as LocationItem;
                        if (UserSettingsService.AppearanceSettingsService.ShowFavoritesSection && section == null)
                        {
                            section = new LocationItem()
                            {
                                Text = "SidebarFavorites".GetLocalized(),
                                Section = SectionType.Favorites,
                                MenuOptions = new ContextMenuOptions
                                {
                                    ShowHideSection = true
                                },
                                SelectsOnInvoked = false,
                                Font = App.MainViewModel.FontName,
                                ChildItems = new BulkConcurrentObservableCollection<INavigationControlItem>()
                            };
                            var index = sectionOrder.TakeWhile(x => x != sectionType).Select(x => SideBarItems.Any(item => item.Section == x) ? 1 : 0).Sum();
                            SideBarItems.Insert(Math.Min(index, SideBarItems.Count), section);
                            section.Icon = await UIHelpers.GetIconResource(Constants.Shell32.QuickAccess); // After insert
                        }
                        return section;
                    }

                case SectionType.Library:
                    {
                        var section = SideBarItems.FirstOrDefault(x => x.Text == "SidebarLibraries".GetLocalized()) as LocationItem;
                        if (UserSettingsService.AppearanceSettingsService.ShowLibrarySection && section == null)
                        {
                            section = new LocationItem
                            {
                                Text = "SidebarLibraries".GetLocalized(),
                                Section = SectionType.Library,
                                MenuOptions = new ContextMenuOptions
                                {
                                    IsLibrariesHeader = true,
                                    ShowHideSection = true
                                },
                                SelectsOnInvoked = false,
                                ChildItems = new BulkConcurrentObservableCollection<INavigationControlItem>()
                            };
                            var index = sectionOrder.TakeWhile(x => x != sectionType).Select(x => SideBarItems.Any(item => item.Section == x) ? 1 : 0).Sum();
                            SideBarItems.Insert(Math.Min(index, SideBarItems.Count), section);
                            section.Icon = await UIHelpers.GetIconResource(Constants.ImageRes.Libraries); // After insert
                        }
                        return section;
                    }

                case SectionType.Drives:
                    {
                        var section = SideBarItems.FirstOrDefault(x => x.Text == "Drives".GetLocalized()) as LocationItem;
                        if (UserSettingsService.AppearanceSettingsService.ShowDrivesSection && section == null)
                        {
                            section = new LocationItem()
                            {
                                Text = "Drives".GetLocalized(),
                                Section = SectionType.Drives,
                                MenuOptions = new ContextMenuOptions
                                {
                                    ShowHideSection = true
                                },
                                SelectsOnInvoked = false,
                                ChildItems = new BulkConcurrentObservableCollection<INavigationControlItem>()
                            };
                            var index = sectionOrder.TakeWhile(x => x != sectionType).Select(x => SideBarItems.Any(item => item.Section == x) ? 1 : 0).Sum();
                            SideBarItems.Insert(Math.Min(index, SideBarItems.Count), section);
                            section.Icon = await UIHelpers.GetIconResource(Constants.ImageRes.ThisPC); // After insert
                        }
                        return section;
                    }

                case SectionType.CloudDrives:
                    {
                        var section = SideBarItems.FirstOrDefault(x => x.Text == "SidebarCloudDrives".GetLocalized()) as LocationItem;
                        if (UserSettingsService.AppearanceSettingsService.ShowCloudDrivesSection && section == null && App.CloudDrivesManager.Drives.Any())
                        {
                            section = new LocationItem()
                            {
                                Text = "SidebarCloudDrives".GetLocalized(),
                                Section = SectionType.CloudDrives,
                                MenuOptions = new ContextMenuOptions
                                {
                                    ShowHideSection = true
                                },
                                SelectsOnInvoked = false,
                                Icon = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/FluentIcons/CloudDrive.png")),
                                ChildItems = new BulkConcurrentObservableCollection<INavigationControlItem>()
                            };
                            var index = sectionOrder.TakeWhile(x => x != sectionType).Select(x => SideBarItems.Any(item => item.Section == x) ? 1 : 0).Sum();
                            SideBarItems.Insert(Math.Min(index, SideBarItems.Count), section);
                        }
                        return section;
                    }

                case SectionType.Network:
                    {
                        var section = SideBarItems.FirstOrDefault(x => x.Text == "SidebarNetworkDrives".GetLocalized()) as LocationItem;
                        if (UserSettingsService.AppearanceSettingsService.ShowNetworkDrivesSection && section == null)
                        {
                            section = new LocationItem()
                            {
                                Text = "SidebarNetworkDrives".GetLocalized(),
                                Section = SectionType.Network,
                                MenuOptions = new ContextMenuOptions
                                {
                                    ShowHideSection = true
                                },
                                SelectsOnInvoked = false,
                                ChildItems = new BulkConcurrentObservableCollection<INavigationControlItem>()
                            };
                            var index = sectionOrder.TakeWhile(x => x != sectionType).Select(x => SideBarItems.Any(item => item.Section == x) ? 1 : 0).Sum();
                            SideBarItems.Insert(Math.Min(index, SideBarItems.Count), section);
                            section.Icon = await UIHelpers.GetIconResource(Constants.ImageRes.NetworkDrives); // After insert
                        }
                        return section;
                    }

                case SectionType.WSL:
                    {
                        var section = SideBarItems.FirstOrDefault(x => x.Text == "WSL".GetLocalized()) as LocationItem;
                        if (UserSettingsService.AppearanceSettingsService.ShowWslSection && section == null && App.WSLDistroManager.Distros.Any())
                        {
                            section = new LocationItem()
                            {
                                Text = "WSL".GetLocalized(),
                                Section = SectionType.WSL,
                                MenuOptions = new ContextMenuOptions
                                {
                                    ShowHideSection = true
                                },
                                SelectsOnInvoked = false,
                                Icon = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/WSL/genericpng.png")),
                                ChildItems = new BulkConcurrentObservableCollection<INavigationControlItem>()
                            };
                            var index = sectionOrder.TakeWhile(x => x != sectionType).Select(x => SideBarItems.Any(item => item.Section == x) ? 1 : 0).Sum();
                            SideBarItems.Insert(Math.Min(index, SideBarItems.Count), section);
                        }
                        return section;
                    }

                case SectionType.FileTag:
                    {
                        var section = SideBarItems.FirstOrDefault(x => x.Text == "FileTags".GetLocalized()) as LocationItem;
                        if (UserSettingsService.PreferencesSettingsService.AreFileTagsEnabled && UserSettingsService.AppearanceSettingsService.ShowFileTagsSection && section == null)
                        {
                            section = new LocationItem()
                            {
                                Text = "FileTags".GetLocalized(),
                                Section = SectionType.FileTag,
                                MenuOptions = new ContextMenuOptions
                                {
                                    ShowHideSection = true
                                },
                                SelectsOnInvoked = false,
                                Icon = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/FluentIcons/FileTags.png")),
                                ChildItems = new BulkConcurrentObservableCollection<INavigationControlItem>()
                            };
                            var index = sectionOrder.TakeWhile(x => x != sectionType).Select(x => SideBarItems.Any(item => item.Section == x) ? 1 : 0).Sum();
                            SideBarItems.Insert(Math.Min(index, SideBarItems.Count), section);
                        }
                        return section;
                    }
                default:
                    return null;
            }
        }

        public async void UpdateSectionVisibility(SectionType sectionType, bool show)
        {
            if (show)
            {
                var appearanceSettingsService = UserSettingsService.AppearanceSettingsService;

                Func<Task> action = sectionType switch
                {
                    SectionType.CloudDrives when appearanceSettingsService.ShowCloudDrivesSection => App.CloudDrivesManager.UpdateDrivesAsync,
                    SectionType.Drives => App.DrivesManager.UpdateDrivesAsync,
                    SectionType.Network when appearanceSettingsService.ShowNetworkDrivesSection => App.NetworkDrivesManager.UpdateDrivesAsync,
                    SectionType.WSL when appearanceSettingsService.ShowWslSection => App.WSLDistroManager.UpdateDrivesAsync,
                    SectionType.FileTag when appearanceSettingsService.ShowFileTagsSection => App.FileTagsManager.UpdateFileTagsAsync,
                    SectionType.Library => App.LibraryManager.UpdateLibrariesAsync,
                    SectionType.Favorites => AddPinnedItemsToSidebarAsync,
                    _ => () => Task.CompletedTask
                };
                await DataChangedAsync(sectionType, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                await action();
            }
            else
            {
                SideBarItems.Remove(SideBarItems.FirstOrDefault(x => x.Section == sectionType));
            }
        }

        public async void EmptyRecycleBin(RoutedEventArgs e)
        {
            await RecycleBinHelpers.S_EmptyRecycleBin();
        }

        private void UserSettingsService_OnSettingChangedEvent(object sender, SettingChangedEventArgs e)
        {
            switch (e.SettingName)
            {
                case nameof(UserSettingsService.AppearanceSettingsService.IsSidebarOpen):
                    if (UserSettingsService.AppearanceSettingsService.IsSidebarOpen != IsSidebarOpen)
                    {
                        OnPropertyChanged(nameof(IsSidebarOpen));
                    }
                    break;
                case nameof(UserSettingsService.AppearanceSettingsService.ShowFavoritesSection):
                    UpdateSectionVisibility(SectionType.Favorites, ShowFavoritesSection);
                    OnPropertyChanged(nameof(ShowFavoritesSection));
                    break;
                case nameof(UserSettingsService.AppearanceSettingsService.ShowLibrarySection):
                    UpdateSectionVisibility(SectionType.Library, ShowLibrarySection);
                    OnPropertyChanged(nameof(ShowLibrarySection));
                    break;
                case nameof(UserSettingsService.AppearanceSettingsService.ShowCloudDrivesSection):
                    UpdateSectionVisibility(SectionType.CloudDrives, ShowCloudDrivesSection);
                    OnPropertyChanged(nameof(ShowCloudDrivesSection));
                    break;
                case nameof(UserSettingsService.AppearanceSettingsService.ShowDrivesSection):
                    UpdateSectionVisibility(SectionType.Drives, ShowDrivesSection);
                    OnPropertyChanged(nameof(ShowDrivesSection));
                    break;
                case nameof(UserSettingsService.AppearanceSettingsService.ShowNetworkDrivesSection):
                    UpdateSectionVisibility(SectionType.Network, ShowNetworkDrivesSection);
                    OnPropertyChanged(nameof(ShowNetworkDrivesSection));
                    break;
                case nameof(UserSettingsService.AppearanceSettingsService.ShowWslSection):
                    UpdateSectionVisibility(SectionType.WSL, ShowWslSection);
                    OnPropertyChanged(nameof(ShowWslSection));
                    break;
                case nameof(UserSettingsService.AppearanceSettingsService.ShowFileTagsSection):
                    UpdateSectionVisibility(SectionType.FileTag, ShowFileTagsSection);
                    OnPropertyChanged(nameof(ShowFileTagsSection));
                    break;
                case nameof(UserSettingsService.PreferencesSettingsService.AreFileTagsEnabled):
                    OnPropertyChanged(nameof(AreFileTagsEnabled));
                    break;
                case nameof(UserSettingsService.AppearanceSettingsService.UseCompactStyles):
                    new SettingsViewModels.AppearanceViewModel().SetCompactStyles(true);
                    break;
            }
        }

        private async Task AddLocationItemToSidebarAsync(LocationItem locationItem)
        {
            int insertIndex = -1;
            lock (favoriteList)
            {
                if (favoriteList.Any(x => x.Path == locationItem.Path))
                {
                    return;
                }
                var lastItem = favoriteList.LastOrDefault(x => x.ItemType == NavigationControlItemType.Location && !string.IsNullOrWhiteSpace(x.Path) && !x.Path.Equals(CommonPaths.RecycleBinPath));
                insertIndex = lastItem != null ? favoriteList.IndexOf(lastItem) + 1 : 0;
                favoriteList.Insert(insertIndex, locationItem);
            }
            await DataChangedAsync(SectionType.Favorites, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, locationItem, insertIndex));
        }

        public async Task AddPinnedItemsToSidebarAsync()
        {
            if (!UserSettingsService.AppearanceSettingsService.ShowFavoritesSection)
            {
                return;
            }

            favoriteList.Clear();

            var homeSection = new LocationItem()
            {
                Text = "Home".GetLocalized(),
                Section = SectionType.Home,
                MenuOptions = new ContextMenuOptions
                {
                    IsLocationItem = true
                },
                Font = App.MainViewModel.FontName,
                IsDefaultLocation = true,
                Icon = await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => new BitmapImage(new Uri("ms-appx:///Assets/FluentIcons/Home.png"))),
                Path = "Home".GetLocalized()
            };
            await AddLocationItemToSidebarAsync(homeSection);

            foreach (LocationItem li in await pinnedItemsService.GetPinnedItemsAsync())
            {
                await AddLocationItemToSidebarAsync(li);
            }

            await ShowHideRecycleBinItemAsync(UserSettingsService.AppearanceSettingsService.PinRecycleBinToSidebar);
        }

        public async Task AddItemAsync(LocationItem item)
        {
            // add to `FavoriteItems` and `favoritesList` must be atomic
            await addSyncSemaphore.WaitAsync();

            try
            {
                if (item is not null && !string.IsNullOrEmpty(item.Path) && !Favorites.Contains(item))
                {
                    await pinnedItemsService.AddPinnedItemAsync(item);
                    //await AddLocationItemToSidebarAsync(item);
                }
            }
            finally
            {
                addSyncSemaphore.Release();
            }
        }

        public async Task AddItemAsync(string path)
        {
            // add to `FavoriteItems` and `favoritesList` must be atomic
            await addSyncSemaphore.WaitAsync();

            try
            {
                if (!string.IsNullOrEmpty(path) && !Favorites.Any(x => x.Path.Equals(path)))
                {
                    await pinnedItemsService.AddPinnedItemByPathAsync(path);
                    //await AddLocationItemToSidebarAsync(item);
                }
            }
            finally
            {
                addSyncSemaphore.Release();
            }
        }

        public async Task ShowHideRecycleBinItemAsync(bool show)
        {
            if (show)
            {
                var recycleBinItem = new LocationItem
                {
                    Text = ApplicationData.Current.LocalSettings.Values.Get("RecycleBin_Title", "Recycle Bin"),
                    IsDefaultLocation = true,
                    MenuOptions = new ContextMenuOptions
                    {
                        IsLocationItem = true,
                        ShowUnpinItem = true,
                        ShowShellItems = true,
                        ShowEmptyRecycleBin = true
                    },
                    Icon = await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => UIHelpers.GetIconResource(Constants.ImageRes.RecycleBin)),
                    Path = CommonPaths.RecycleBinPath
                };
                // Add recycle bin to sidebar, title is read from LocalSettings (provided by the fulltrust process)
                // TODO: the very first time the app is launched localized name not available
                lock (favoriteList)
                {
                    if (favoriteList.Any(x => x.Path == CommonPaths.RecycleBinPath))
                    {
                        return;
                    }
                    favoriteList.Add(recycleBinItem);
                }
                await DataChangedAsync(SectionType.Favorites, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, recycleBinItem));
            }
            else
            {
                foreach (INavigationControlItem item in Favorites)
                {
                    if (item is LocationItem && item.Path == CommonPaths.RecycleBinPath)
                    {
                        lock (favoriteList)
                        {
                            favoriteList.Remove(item);
                        }
                        await DataChangedAsync(SectionType.Favorites, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
                    }
                }
            }
        }

        public async Task RemoveItemAsync(LocationItem item)
        {
            await pinnedItemsService.RemovePinnedItemAsync(item);
            await DataChangedAsync(SectionType.Favorites, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove));
        }

        public async Task<bool> MoveItemAsync(LocationItem locationItem, int oldIndex, int newIndex)
        {
            if (locationItem == null)
            {
                return false;
            }

            if (oldIndex > 0 && newIndex > 0 && newIndex <= Favorites.Count)
            {
                // A backup of the items, because the swapping of items requires removing and inserting them in the correct position
                var sidebarItemsBackup = new List<INavigationControlItem>(Favorites);

                try
                {
                    pinnedItemsService.RemovePinnedItemAt(oldIndex - 1);
                    await pinnedItemsService.AddPinnedItemAsync(locationItem, newIndex - 1);
                    lock (favoriteList)
                    {
                        favoriteList.RemoveAt(oldIndex);
                        favoriteList.Insert(newIndex, locationItem);
                    }
                    await DataChangedAsync(SectionType.Favorites, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, locationItem, newIndex, oldIndex));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"An error occurred while moving pinned items in the Favorites sidebar section. {ex.Message}");
                    favoriteList = sidebarItemsBackup;
                    await AddPinnedItemsToSidebarAsync();
                    return false;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Swaps two location items in the navigation sidebar
        /// </summary>
        /// <param name="firstLocationItem">The first location item</param>
        /// <param name="secondLocationItem">The second location item</param>
        public async Task SwapItemsAsync(LocationItem firstLocationItem, INavigationControlItem secondLocationItem)
        {
            if (firstLocationItem == null || secondLocationItem == null)
            {
                return;
            }

            var indexOfFirstItemInMainPage = IndexOfItem(firstLocationItem);
            var indexOfSecondItemInMainPage = IndexOfItem(secondLocationItem);

            // Moves the items in the MainPage
            await MoveItemAsync(firstLocationItem, indexOfFirstItemInMainPage, indexOfSecondItemInMainPage);
        }

        /// <summary>
        /// Returns the index of the location item in the navigation sidebar
        /// </summary>
        /// <param name="locationItem">The location item</param>
        /// <returns>Index of the item</returns>
        public int IndexOfItem(INavigationControlItem locationItem)
        {
            lock (favoriteList)
            {
                return favoriteList.FindIndex(x => x.Path == locationItem.Path);
            }
        }

        /// <summary>
        /// Returns the index of the location item in the collection containing Navigation control items
        /// </summary>
        /// <param name="locationItem">The location item</param>
        /// <param name="collection">The collection in which to find the location item</param>
        /// <returns>Index of the item</returns>
        public int IndexOfItem(INavigationControlItem locationItem, List<INavigationControlItem> collection)
        {
            return collection.IndexOf(locationItem);
        }


        public void Dispose()
        {
            UserSettingsService.OnSettingChangedEvent -= UserSettingsService_OnSettingChangedEvent;

            App.LibraryManager.DataChanged -= async (x, y) => await DataChangedAsync((SectionType)x, y);
            App.DrivesManager.DataChanged -= async (x, y) => await DataChangedAsync((SectionType)x, y);
            App.CloudDrivesManager.DataChanged -= async (x, y) => await DataChangedAsync((SectionType)x, y);
            App.NetworkDrivesManager.DataChanged -= async (x, y) => await DataChangedAsync((SectionType)x, y);
            App.WSLDistroManager.DataChanged -= async (x, y) => await DataChangedAsync((SectionType)x, y);
            App.FileTagsManager.DataChanged -= async (x, y) => await DataChangedAsync((SectionType)x, y);
        }

        public void SidebarControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
        {
            SidebarDisplayMode = args.DisplayMode;
        }

        public void UpdateTabControlMargin()
        {
            TabControlMargin = SidebarDisplayMode switch
            {
                // This prevents the pane toggle button from overlapping the tab control in minimal mode
                NavigationViewDisplayMode.Minimal => new GridLength(44, GridUnitType.Pixel),
                _ => new GridLength(0, GridUnitType.Pixel),
            };
        }

        private GridLength tabControlMargin;

        public GridLength TabControlMargin
        {
            get => tabControlMargin;
            set => SetProperty(ref tabControlMargin, value);
        }
    }
}