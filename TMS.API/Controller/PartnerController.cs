using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.API.Controller;
using TMS.API.ModelACs;

namespace TMS.API.Controllers
{
    public class PartnerController : ACController<Partner>
    {
        public PartnerController(ACContext context, API.Models.TMSContext tMSContext, EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, tMSContext, entityService, httpContextAccessor)
        {
        }
    }
}
