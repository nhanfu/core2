using Microsoft.AspNetCore.Http;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class FileUploadController : TMSController<FileUpload>
    {
        public FileUploadController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {

        }
    }
}
