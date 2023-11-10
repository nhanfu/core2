using Core.Extensions;
using Microsoft.AspNetCore.Authorization;
using Core.Controllers;
using Core.Models;

namespace Core.Controller
{
    [Authorize]
    public class ServiceController<T> : GenericController<T> where T : class
    {
        protected readonly LOGContext db;

        public ServiceController(LOGContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
            db = context;
        }
    }
}
