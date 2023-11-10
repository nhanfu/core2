using Core.Extensions;
using System;
using System.Linq;
using Core.Models;

namespace Core.Services
{
    public class VendorSvc
    {
        private readonly TMSContext db;
        private readonly UserService _userSvc;
        protected readonly EntityService _entitySvc;

        public VendorSvc(UserService userService, TMSContext db, EntityService entityService)
        {
            _userSvc = userService ?? throw new ArgumentNullException(nameof(userService));
            _entitySvc = entityService;
            this.db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public IQueryable<Vendor> GetVendor()
        {
            var equ =
                from vendor in db.Vendor
                select vendor;
            return equ;
        }
    }
}
