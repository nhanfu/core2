using Core.Extensions;
using Core.Models;

namespace Core.Controllers
{
    public class FileUploadController : TMSController<FileUpload>
    {
        public FileUploadController(CoreContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {

        }
    }
}
