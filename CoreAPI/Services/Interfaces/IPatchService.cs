using Core.ViewModels;
using CoreAPI.Services.Sql;

namespace CoreAPI.Services.Interfaces
{
    public interface IPatchService
    {
        Task<int> SavePatch(PatchVM vm);
        Task<int> UpdatePatch(PatchVM vm);
        Task<SqlResult> SavePatch2(PatchVM vm);
        Task<SqlResult> SavePatchs2(List<PatchVM> vms);
        Task<int> SavePatches(PatchVM[] patches);
        Task<bool> HardDelete(PatchVM vm);
        Task<string[]> DeactivateAsync(SqlViewModel vm);
    }
}
