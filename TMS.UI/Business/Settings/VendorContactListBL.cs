using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Enums;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class VendorContactListBL : TabEditor
    {
        public VendorContactListBL() : base(nameof(VendorContact))
        {
            Name = "VendorContact List";
        }

        public async Task EditVendorContact(VendorContact entity)
        {
            await this.OpenPopup(
                featureName: "VendorContact Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.VendorContactEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thông tin liên hệ";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public async Task AddVendorContact()
        {
            await this.OpenPopup(
                featureName: "VendorContact Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.VendorContactEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới thông tin liên hệ";
                    instance.Entity = new VendorContact();
                    return instance;
                });
        }
    }
}
