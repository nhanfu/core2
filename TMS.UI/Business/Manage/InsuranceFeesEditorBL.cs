using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.MVVM;
using Core.ViewModels;
using Retyped.Primitive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using static Retyped.dom.Literals.Types;
using Number = Core.Components.Number;

namespace TMS.UI.Business.Manage
{
    public class InsuranceFeesEditorBL : PopupEditor
    {
        public Expense expenseEntity => Entity as Expense;
        public InsuranceFeesEditorBL() : base(nameof(Expense))
        {
            Name = "InsuranceFees Editor";
        }

        public void SelectedCompare(Expense expense)
        {
            CompareChanges(expense, expenseEntity);
        }

        private void CompareChanges(object change, object cutting)
        {
            if (change != null)
            {
                var listItem = change.GetType().GetProperties();
                var content = this.FindComponentByName<Section>("Wrapper");
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
                        //listViewItem.FilterChildren(x => x.Name == item.Name).FirstOrDefault()?.Element?.AddClass("text-warning");
                    }
                }
            }
        }

        public override void Reject()
        {
            var confirm = new ConfirmDialog
            {
                NeedAnswer = true,
                ComType = nameof(Textbox),
                Content = $"Bạn có chắc chắn muốn trả về?<br />" +
                    "Hãy nhập lý do trả về",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                var _gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
                var listViewItem = _gridView.RowData.Data.Cast<Expense>().FirstOrDefault(x => x.StatusId == (int)ApprovalStatusEnum.Approving);
                listViewItem.ClearReferences();
                var res = await Client.CreateAsync<object>(listViewItem, "Reject?reasonOfChange=" + confirm.Textbox?.Text);
                ProcessEnumMessage(res);
            };
        }

        public async Task ApproveRequestChange()
        {
            var isValid = await IsFormValid();
            if (!isValid)
            {
                return;
            }
            var confirm = new ConfirmDialog
            {
                Content = "Bạn có chắc chắn phê duyệt?",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                var _gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
                var listViewItem = _gridView.GetSelectedRows().Cast<Expense>().FirstOrDefault(x => x.StatusId == (int)ApprovalStatusEnum.Approving);
                var containerTypeId = await CheckContainerType(listViewItem);
                var commodidtyValue = await new Client(nameof(CommodityValue)).FirstOrDefaultAsync<CommodityValue>($"?$filter=Active eq true and BossId eq {listViewItem.BossId} and CommodityId eq {listViewItem.CommodityId} and ContainerId eq {containerTypeId}");
                if (commodidtyValue is null && listViewItem.BossId != null && listViewItem.CommodityId != null && listViewItem.ContainerTypeId != null)
                {
                    var newCommodityValue = await CreateCommodityValue(listViewItem);
                    await new Client(nameof(CommodityValue)).CreateAsync<CommodityValue>(newCommodityValue);
                }
                var history = new Expense();
                history.CopyPropFrom(expenseEntity);
                history.Id = 0;
                history.StatusId = (int)ApprovalStatusEnum.New;
                history.RequestChangeId = expenseEntity.Id;
                await new Client(nameof(Expense)).CreateAsync<Expense>(history);
                expenseEntity.IsHasChange = true;
                await new Client(nameof(Expense)).PatchAsync<Expense>(GetPatchEntity(expenseEntity));
                listViewItem.ClearReferences();
                await Approve(listViewItem);
            };
        }

        private int containerId = 0;

        public async Task<int> CheckContainerType(Expense expense)
        {
            var containerTypes = await new Client(nameof(MasterData)).GetRawList<MasterData>($"?$filter=Active eq true and ParentId eq 7565");
            var containerTypeCodes = containerTypes.ToDictionary(x => x.Id);
            var containerTypeName = containerTypeCodes.GetValueOrDefault((int)expense.ContainerTypeId);
            var containers = await new Client(nameof(MasterData)).GetRawList<MasterData>($"?$filter=Active eq true and (contains(Name, '40HC') or contains(Name, '20DC') or contains(Name, '45HC') or contains(Name, '50DC'))");
            if (containerTypeName.Description.Contains("Cont 20"))
            {
                containerId = containers.Find(x => x.Name.Contains("20DC")).Id;
            }
            else if (containerTypeName.Description.Contains("Cont 40"))
            {
                containerId = containers.Find(x => x.Name.Contains("40HC")).Id;
            }
            else if (containerTypeName.Description.Contains("Cont 45"))
            {
                containerId = containers.Find(x => x.Name.Contains("45HC")).Id;
            }
            else if (containerTypeName.Description.Contains("Cont 50"))
            {
                containerId = containers.Find(x => x.Name.Contains("50DC")).Id;
            }
            return containerId;
        }

        private async Task<CommodityValue> CreateCommodityValue(Expense expense)
        {
            var startDate1 = new DateTime(DateTime.Now.Year, 1, 1);
            var endDate1 = new DateTime(DateTime.Now.Year, 6, 30);
            var startDate2 = new DateTime(DateTime.Now.Year, 7, 1);
            var endDate2 = new DateTime(DateTime.Now.Year, 12, 31);
            var containerId = await CheckContainerType(expense);
            var newCommodityValue = new CommodityValue();
            newCommodityValue.CopyPropFrom(expense);
            newCommodityValue.Id = 0;
            newCommodityValue.ContainerId = containerId;
            newCommodityValue.TotalPrice = (decimal)expense.CommodityValue;
            newCommodityValue.StartDate = DateTime.Now.Date;
            newCommodityValue.Notes = "";
            newCommodityValue.Active = true;
            newCommodityValue.InsertedDate = DateTime.Now.Date;
            newCommodityValue.CreatedBy = Client.Token.UserId;
            if (DateTime.Now.Date >= startDate1 && DateTime.Now.Date <= endDate1)
            {
                newCommodityValue.EndDate = endDate1;
            }
            if (DateTime.Now.Date >= startDate2 && DateTime.Now.Date <= endDate2)
            {
                newCommodityValue.EndDate = endDate2;
            }
            return newCommodityValue;
        }

        public PatchUpdate GetPatchEntity(Expense expense)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = expense.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Expense.IsHasChange), Value = expense.IsHasChange.ToString() });
            return new PatchUpdate { Changes = details };
        }
    }
}