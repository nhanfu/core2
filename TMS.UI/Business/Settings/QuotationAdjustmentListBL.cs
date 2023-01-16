using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.MVVM;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class QuotationAdjustmentListBL : TabEditor
    {
        public QuotationAdjustmentListBL() : base(nameof(Quotation))
        {
            Name = "QuotationAdjustment List";
        }

        public async Task EditQuotationAdjustment(Quotation entity)
        {
            await this.OpenPopup(
                featureName: "QuotationAdjustment Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.QuotationAdjustmentEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa điều chỉnh cước tàu";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public async Task AddQuotationAdjustment()
        {
            await this.OpenPopup(
                featureName: "QuotationAdjustment Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.QuotationAdjustmentEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới điều chỉnh cước tàu";
                    instance.Entity = new Quotation()
                    {
                        TypeId = 11483
                    };
                    return instance;
                });
        }
    }
}