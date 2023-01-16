using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Models;

namespace Core.Fw.User
{
    public class VendorListVM
    {
        public List<Vendor> Vendor { get; set; }
    }

    public class VendorBL : TabEditor
    {
        public VendorBL() : base(nameof(Vendor))
        {
            Entity = new VendorListVM();
            Name = "Vendor List";
            Title = Name;
        }

        public async Task EditVendor(Vendor vendor)
        {
            await InitVendorForm(vendor);
        }

        public async Task CreateVendor()
        {
            await InitVendorForm(new Vendor() { VendorTypeId = (int)VendorTypeEnum.Vendor });
        }

        private async Task InitVendorForm(Vendor vendor)
        {
            await this.OpenTab(id: "Vendor" + vendor.Id,
                featureName: "VendorDetail",
                factory: () =>
                {
                    var type = Type.GetType("Core.Fw.User.VendorDetailBL");
                    var instance = Activator.CreateInstance(type) as TabEditor;
                    instance.Entity = vendor;
                    return instance;
                });
        }
    }
}