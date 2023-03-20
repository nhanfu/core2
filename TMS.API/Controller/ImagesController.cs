using Core.Extensions;
using Microsoft.AspNetCore.Mvc;
using TMS.API.Models;
using FileIO = System.IO.File;

namespace TMS.API.Controllers
{
    public class ImagesController : TMSController<Images>
    {
        public ImagesController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }

        [HttpPost("api/[Controller]/UploadImage")]
        public async Task<Images> UploadImageAsync([FromServices] IWebHostEnvironment host, [FromBody] string image, string path = "\\upload\\images", string filename = "")
        {
            var pth = host.WebRootPath + path + "\\" + filename;
            EnsureDirectoryExist(pth);
            if (!FileIO.Exists(pth))
            {
                var bytes = Convert.FromBase64String(image);
                await FileIO.WriteAllBytesAsync(pth, bytes);
                var img = new Images()
                {
                    Url = path + "\\" + filename,
                    Name = filename,
                    Size = bytes.Length.ToString(),
                };
                SetAuditInfo(img);
                db.Add(img);
                await db.SaveChangesAsync();
                return img;
            }
            else
            {
                //var img = await db.Images.FirstOrDefaultAsync(x => x.Name == filename);
                return new Images();
            }
        }
    }
}
