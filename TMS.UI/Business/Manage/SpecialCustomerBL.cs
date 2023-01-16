using Core.Components.Extensions;
using Core.Components.Forms;
using System;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Manage
{
    public class SpecialCustomerBL : TabEditor
    {
        public SpecialCustomerBL() : base(nameof(Vendor))
        {
            Name = "SpecialCustomer List";
        }

        public async Task EditSpecialCustomer(Vendor entity)
        {
            await this.OpenPopup(
                featureName: "SpecialCustomer Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.SpecialCustomerEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa khách hàng đặc biệt";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public async Task AddSpecialCustomer()
        {
            await this.OpenPopup(
                featureName: "SpecialCustomer Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.SpecialCustomerEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới khách hàng đặc biệt";
                    instance.Entity = new Vendor()
                    {
                        TypeId = 12095,
                        ReturnRate = 0
                    };
                    return instance;
                });
        }
    }
}