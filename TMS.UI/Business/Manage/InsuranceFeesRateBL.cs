using Core.Components.Extensions;
using Core.Components.Forms;
using System;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Manage
{
    public class InsuranceFeesRateBL : TabEditor
    {
        public InsuranceFeesRateBL() : base(nameof(InsuranceFeesRate))
        {
            Name = "InsuranceFeesRate List";
        }

        public async Task EditInsuranceFeesRate(InsuranceFeesRate entity)
        {
            await this.OpenPopup(
                featureName: "InsuranceFeesRate Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.InsuranceFeesRateEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa tỷ lệ phí BH";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public async Task AddInsuranceFeesRate()
        {
            await this.OpenPopup(
                featureName: "InsuranceFeesRate Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.InsuranceFeesRateEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới tỷ lệ phí BH";
                    instance.Entity = new InsuranceFeesRate();
                    return instance;
                });
        }
    }
}