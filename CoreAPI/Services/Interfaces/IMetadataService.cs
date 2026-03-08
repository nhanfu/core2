using Core.Models;

namespace CoreAPI.Services.Interfaces
{
    public interface IMetadataService
    {
        Task<Dictionary<string, object>[]> GetMenu();
        Feature LoadFeature(string name);
    }
}
