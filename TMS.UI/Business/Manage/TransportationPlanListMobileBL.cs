using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using System;
using TMS.API.Enums;
using Core.Clients;
using System.Collections.Generic;
using Bridge.Html5;
using Core.MVVM;
using Core.Enums;

namespace TMS.UI.Business.Manage
{
    public class TransportationPlanListMobileBL : TabEditor
    {
        public TransportationPlanListMobileBL() : base(nameof(TransportationPlan))
        {
            Name = "TransportationPlan Mobile List";
        }

        public async Task EditTransportationPlan(TransportationPlan entity)
        {
            var id = "EditTransportationPlanMobile" + entity.Id;
            await this.OpenTab(
                id: id,
                featureName: "TransportationPlan Editor Mobile",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.TransportationPlanEditorMobileBL");
                    var instance = Activator.CreateInstance(type) as TabEditor;
                    instance.Title = "Chỉnh sửa KHVC";
                    instance.Icon = "fal fa-sitemap mr-1";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public async Task AddTransportationPlan()
        {
            await this.OpenTab(
                id: "AddTransportationPlanMobile",
                featureName: "TransportationPlan Editor Mobile",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.TransportationPlanEditorMobileBL");
                    var instance = Activator.CreateInstance(type) as TabEditor;
                    instance.Title = "Thêm mới KHVC";
                    instance.Icon = "fal fa-sitemap mr-1";
                    instance.Entity = new TransportationPlan();
                    return instance;
                });
        }
    }
}
