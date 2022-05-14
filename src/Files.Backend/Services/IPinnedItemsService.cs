using Files.Backend.DataModels.NavigationControlItems;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.Backend.Services
{
    public interface IPinnedItemsService
    {
        Task<IList<LocationItemBase>> GetPinnedItemsAsync();

        Task<List<string>> GetPinnedItemPathsAsync();

        Task AddPinnedItemAsync(LocationItemBase item, int index = -1);

        Task AddPinnedItemByPathAsync(string path, int index = -1);

        Task AddPinnedItemsByPathAsync(IList<string> paths);

        Task<bool> CheckPinnedStatusByPathAsync(string path);

        void RefreshPinnedItems();

        Task RemovePinnedItemAsync(LocationItemBase item);

        Task RemovePinnedItemByPathAsync(string path);

        Task RemovePinnedItemsByPathAsync(IList<string> paths);

        void RemovePinnedItemAt(int index);
    }
}
