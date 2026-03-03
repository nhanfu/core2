namespace CoreAPI.Services.Interfaces
{
    public interface IFileService
    {
        Task<string> PostImageAsync(IWebHostEnvironment host, string name = "Captured", bool reup = false);
        Task<string> PostFileAsync(IFormFile file, bool reup = false);
        Task<bool> ImportCsv(List<IFormFile> files, string table, string comId, string connKey);
        string GetUploadPath(string fileName, string webRootPath);
        string GetHttpPath(string path, string webRootPath);
        string IncreaseFileName(string path);
        void EnsureDirectoryExist(string path);
    }
}
