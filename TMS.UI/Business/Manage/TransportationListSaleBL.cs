using Core.Components.Extensions;
using Core.Components;
using Core.Components.Forms;
using Core.Extensions;
using System;
using TMS.API.Models;
using System.Linq;
using Core.Clients;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.ViewModels;

namespace TMS.UI.Business.Manage
{
    public class TransportationListSaleBL : TabEditor
    {
        public Transportation selected;
        public TransportationListSaleBL() : base(nameof(Transportation))
        {
            Name = "Transportation List Sale";
        }

        public async Task ReloadExpense(Transportation transportation)
        {
            var grid = this.FindComponentByName<GridView>(nameof(Expense));
            grid.DataSourceFilter = $"?$filter=Active eq true and TransportationId eq {transportation.Id} and IsReturn eq false and (ExpenseTypeId in (15981, 15939) eq false or IsPurchasedInsurance eq true) and RequestChangeId eq null";
            selected = transportation;
            await grid.ApplyFilter(true);
        }
    }
}