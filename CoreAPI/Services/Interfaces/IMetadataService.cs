using Core.Models;

namespace CoreAPI.Services.Interfaces
{
    public interface IMetadataService
    {
        Task<Dictionary<string, object>[]> GetMenu();
        Task<bool> PublishAllFeature(string t);
        Task<bool> PublishFeatureByName(string Name, string t = null);
        Feature GetFeature(string name);
    }
}
