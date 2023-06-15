using Microsoft.AspNetCore.Mvc;
using FileIO = System.IO.File;

namespace FTP.Controllers
{
    public class FileUploadController : ControllerBase
    {
        private readonly ILogger<FileUploadController> _logger;

        public FileUploadController(ILogger<FileUploadController> logger)
        {
            _logger = logger;
        }

        [HttpPost("api/FileUpload/File")]
        public async Task<string> PostFileAsync([FromServices] IWebHostEnvironment host, [FromForm] IFormFile file, [FromQuery] string tanentcode, [FromQuery] int userid, bool reup = false)
        {
            var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var path = GetUploadPath(fileName, host.WebRootPath, tanentcode, userid);
            EnsureDirectoryExist(path);
            path = reup ? IncreaseFileName(path) : path;
            using var stream = FileIO.Create(path);
            await file.CopyToAsync(stream);
            stream.Close();
            return GetRelativePath(path, host.WebRootPath);
        }

        [HttpPost("api/FileUpload/Image")]
        public async Task<string> PostImageAsync([FromServices] IWebHostEnvironment host, [FromBody] string image, [FromQuery] string tanentcode, [FromQuery] int userid, string name = "Captured", bool reup = false)
        {
            var fileName = $"{Path.GetFileNameWithoutExtension(name)}{Guid.NewGuid()}{Path.GetExtension(name)}";
            var path = GetUploadPath(fileName, host.WebRootPath, tanentcode, userid);
            EnsureDirectoryExist(path);
            path = reup ? IncreaseFileName(path) : path;
            await FileIO.WriteAllBytesAsync(path, Convert.FromBase64String(image));
            return GetRelativePath(path, host.WebRootPath);
        }

        [HttpPost("api/[Controller]/DeleteFile")]
        public ActionResult DeleteFile([FromServices] IWebHostEnvironment host, [FromBody] string path)
        {
            var absolutePath = Path.Combine(host.WebRootPath, path);
            if (FileIO.Exists(absolutePath))
            {
                FileIO.Delete(absolutePath);
            }
            return Ok(true);
        }

        private string GetRelativePath(string path, string webRootPath)
        {
            return Request.Scheme + "://" + Request.Host.Value + path.Replace(webRootPath, string.Empty).Replace("\\", "/");
        }

        private void EnsureDirectoryExist(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        private string IncreaseFileName(string path)
        {
            var uploadedPath = path;
            var index = 0;
            while (FileIO.Exists(path))
            {
                var noExtension = Path.GetFileNameWithoutExtension(uploadedPath);
                var dir = Path.GetDirectoryName(uploadedPath);
                index++;
                path = Path.Combine(dir, noExtension + "_" + index + Path.GetExtension(uploadedPath));
            }

            return path;
        }

        private string GetUploadPath(string fileName, string webRootPath, string tanentcode, int userid)
        {
            return Path.Combine(webRootPath, "upload", tanentcode, $"U{userid:00000000}", fileName);
        }
    }
}