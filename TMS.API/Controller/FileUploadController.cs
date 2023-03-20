using Core.Extensions;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class FileUploadController : TMSController<FileUpload>
    {
        public FileUploadController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {

        }
    }
}
