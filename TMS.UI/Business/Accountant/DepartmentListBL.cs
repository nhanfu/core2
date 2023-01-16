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
    public class DepartmentListBL : TabEditor
    {
        public DepartmentListBL() : base(nameof(MasterData))
        {
            Name = "Department List";
        }

        public async Task AddDepartment()
        {
            await this.OpenPopup(
                featureName: "Department Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.DepartmentEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới bộ phận";
                    instance.Entity = new MasterData()
                    {
                        ParentId = 24944
                    };
                    return instance;
                });
        }

        public async Task EditDepartment(MasterData masterData)
        {
            await this.OpenPopup(
                featureName: "Department Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.DepartmentEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa bộ phận";
                    instance.Entity = masterData;
                    return instance;
                });
        }

        public void BeforeCreatedMasterData(MasterData masterData)
        {
            masterData.ParentId = 24944;
        }
    }
}
