using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.MVVM;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.API.ViewModels;

namespace TMS.UI.Business.Accountant
{
    public class FreightRateEditBL : PopupEditor
    {
        public FreightRate freightRateEntity => Entity as FreightRate;
        public FreightRateEditBL() : base(nameof(FreightRate))
        {
            Name = "FreightRate Editor";
        }

        public async Task SetInfo()
        {
            var vendor = await new Client(nameof(Vendor)).FirstOrDefaultAsync<Vendor>($"?$filter=Active eq true and TypeId eq 7551 and Id eq {freightRateEntity.BossId}");
            freightRateEntity.UserId = vendor is null ? null : vendor.UserId;
            UpdateView(false, nameof(FreightRate.UserId));
        }
    }
}
