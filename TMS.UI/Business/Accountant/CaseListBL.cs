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
    public class CaseListBL : TabEditor
    {
        public CaseListBL() : base(nameof(MasterData))
        {
            Name = "Case List";
        }

        public async Task AddCase()
        {
            await this.OpenPopup(
                featureName: "Case Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.CaseEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới vụ việc";
                    instance.Entity = new MasterData()
                    {
                        ParentId = 24945
                    };
                    return instance;
                });
        }

        public async Task EditCase(MasterData masterData)
        {
            await this.OpenPopup(
                featureName: "Case Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.CaseEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa vụ việc";
                    instance.Entity = masterData;
                    return instance;
                });
        }

        public void BeforeCreatedMasterData(MasterData masterData)
        {
            masterData.ParentId = 24945;
        }
    }
}
