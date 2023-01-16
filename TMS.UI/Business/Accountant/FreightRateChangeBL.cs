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
    public class FreightRateChangeBL : PopupEditor
    {
        public FreightRate freightRateEntity => Entity as FreightRate;
        public FreightRateChangeBL() : base(nameof(FreightRate))
        {
            Name = "FreightRate Change";
        }

        public void SelectedCompare(FreightRate freightRate)
        {
            CompareChanges(freightRate, freightRateEntity);
        }

        private void CompareChanges(object change, object cutting)
        {
            if (change != null)
            {
                var listItem = change.GetType().GetProperties();
                var content = this.FindComponentByName<Section>("Wrapper1");
                var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
                var listViewItem = gridView.GetListViewItems(change).FirstOrDefault();
                content.FilterChildren(x => true).ForEach(x => x.ParentElement.RemoveClass("bg-warning"));
                listViewItem.FilterChildren(x => true).ForEach(x => x.Element.RemoveClass("text-warning"));
                foreach (var item in listItem)
                {
                    var a1 = change[item.Name];
                    var a2 = cutting[item.Name];
                    if (a1 == null && a2 == null)
                    {
                        continue;
                    }

                    if (a1 != null && a2 == null || a1 == null && a2 != null || a1 != null && a2 != null && a1.ToString() != a2.ToString())
                    {
                        content.FilterChildren(x => x.Name == item.Name).ForEach(x =>
                        {
                            x.ParentElement.AddClass("bg-warning");
                        });
                        listViewItem.FilterChildren(x => x.Name == item.Name).FirstOrDefault()?.Element?.AddClass("text-warning");
                    }
                }
            }
        }
    }
}
