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
    public class ItemsListBL : TabEditor
    {
        public ItemsListBL() : base(nameof(MasterData))
        {
            Name = "Items List";
        }

        public async Task AddItem()
        {
            await this.OpenPopup(
                featureName: "Item Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.ItemsEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới khoản mục";
                    instance.Entity = new MasterData()
                    {
                        ParentId = 23767
                    };
                    return instance;
                });
        }

        public async Task EditItem(MasterData masterData)
        {
            await this.OpenPopup(
                featureName: "Item Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.ItemsEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa khoản mục";
                    instance.Entity = masterData;
                    return instance;
                });
        }

        public void BeforeCreatedMasterData(MasterData masterData)
        {
            masterData.ParentId = 23767;
        }
    }
}
