using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.API.ViewModels;

namespace TMS.UI.Business.Accountant
{
    public class SaleListBL : TabEditor
    {
        public SaleListBL() : base(nameof(Sale))
        {
            Name = "Sale List";
        }

        public async Task AddSale()
        {
            await this.OpenPopup(
                featureName: "Sale Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.SaleEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới tên nhóm và sale";
                    instance.Entity = new Sale();
                    return instance;
                });
        }

        public async Task EditSale(Sale entity)
        {
            await this.OpenPopup(
                featureName: "Sale Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.SaleEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa tên nhóm và sale";
                    instance.Entity = entity;
                    return instance;
                });
        }
    }
}
