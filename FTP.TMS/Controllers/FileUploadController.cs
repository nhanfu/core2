using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.IO.Pipelines;
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

        [HttpPost("api/FileUpload/Image")]
        public async Task<string> PostImageAsync([FromServices] IWebHostEnvironment host, [FromBody] string image, [FromQuery] string tanentcode, [FromQuery] int userid, string name = "Captured", bool reup = false)
        {
            var fileName = $"{Path.GetFileNameWithoutExtension(name)}{Guid.NewGuid()}{Path.GetExtension(name)}";
            var path = GetUploadPath(fileName, host.WebRootPath, tanentcode, userid);
            EnsureDirectoryExist(path);
            path = reup ? IncreaseFileName(path) : path;

            // Convert the base64 image string to bytes
            byte[] imageBytes = Convert.FromBase64String(image);

            // Create an ImageSharp image from the bytes
            using (Image imageSharp = Image.Load(imageBytes))
            {
                // Resize the image to a target width and height while maintaining aspect ratio
                const int targetWidth = 1200;
                const int targetHeight = 800;
                imageSharp.Mutate(x => x
                    .Resize(new ResizeOptions
                    {
                        Size = new Size(targetWidth, targetHeight),
                        Mode = ResizeMode.Max
                    }));

                // Compress the image to reduce file size
                using (MemoryStream output = new MemoryStream())
                {
                    // Specify JPEG format with compression quality of 80 (adjust as needed)
                    var jpegEncoder = new JpegEncoder { Quality = 80 };
                    imageSharp.Save(output, jpegEncoder);
                    output.Seek(0, SeekOrigin.Begin);

                    // Limit the file size to approximately 1MB
                    const int maxFileSizeInBytes = 1_000_000;
                    if (output.Length > maxFileSizeInBytes)
                    {
                        double scaleFactor = Math.Sqrt((double)maxFileSizeInBytes / output.Length);
                        int resizedWidth = (int)Math.Round(targetWidth * scaleFactor);
                        int resizedHeight = (int)Math.Round(targetHeight * scaleFactor);

                        // Resize the image again with the adjusted dimensions
                        imageSharp.Mutate(x => x
                            .Resize(new ResizeOptions
                            {
                                Size = new Size(resizedWidth, resizedHeight),
                                Mode = ResizeMode.Max
                            }));

                        output.SetLength(0); // Clear the memory stream
                        imageSharp.Save(output, jpegEncoder);
                        output.Seek(0, SeekOrigin.Begin);
                    }

                    // Save the compressed image to the file system
                    using (FileStream fileStream = FileIO.Create(path))
                    {
                        await output.CopyToAsync(fileStream);
                    }
                }
            }

            return GetRelativePath(path, host.WebRootPath);
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
            return Path.Combine(webRootPath, "upload", tanentcode, DateTime.Now.ToString("MMyyyy"), $"U{userid:00000000}", fileName);
        }
    }
}