using Files.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.Uwp.Filesystem.Cloud
{
    public interface ICloudProviderDetector
    {
        Task<IList<CloudProvider>> DetectAsync();
    }
}