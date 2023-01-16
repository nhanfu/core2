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
    public class TaxExpenseItemsListBL : TabEditor
    {
        public TaxExpenseItemsListBL() : base(nameof(MasterData))
        {
            Name = "TaxExpenseItems List";
        }

        public async Task AddTaxExpenseItem()
        {
            await this.OpenPopup(
                featureName: "TaxExpenseItem Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.TaxExpenseItemsEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới K.Mục CP thuế";
                    instance.Entity = new MasterData()
                    {
                        ParentId = 23776
                    };
                    return instance;
                });
        }

        public async Task EditTaxExpenseItem(MasterData masterData)
        {
            await this.OpenPopup(
                featureName: "TaxExpenseItem Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.TaxExpenseItemsEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa K.Mục CP thuế";
                    instance.Entity = masterData;
                    return instance;
                });
        }

        public void BeforeCreatedMasterData(MasterData masterData)
        {
            masterData.ParentId = 23776;
        }
    }
}
