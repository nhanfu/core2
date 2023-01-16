using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using TMS.API.Models;
using FileIO = System.IO.File;

namespace TMS.API.Controllers
{
    public class SettingTransportationController : TMSController<SettingTransportation>
    {
        public SettingTransportationController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }
    }
}
