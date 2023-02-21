using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Manage
{
    public class BookingListListBL : TabEditor
    {
        public BookingListListBL() : base(nameof(BookingList))
        {
            Name = "List Ship Book";
        }

        public void SetLock()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            var listViewItems = gridView.RowData.Data.Cast<BookingList>().ToList();
            listViewItems.ForEach(x =>
            {
                var listViewItem = gridView.GetListViewItems(x).FirstOrDefault();
                if (listViewItem is null)
                {
                    return;
                }
                if (x.Submit)
                {
                    listViewItem.FilterChildren(y => y.GuiInfo.FieldName != "Submit" && !y.GuiInfo.Disabled).ForEach(y => y.Disabled = true);
                }
                else
                {
                    listViewItem.FilterChildren(y => !y.GuiInfo.Disabled).ForEach(y => y.Disabled = false);
                }
            });
        }

        public void ChangeSubmit(BookingList bookingList)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            var listViewItem = gridView.GetListViewItems(bookingList).FirstOrDefault();
            if (listViewItem is null)
            {
                return;
            }
            if (bookingList.Submit)
            {
                listViewItem.FilterChildren(y => y.GuiInfo.FieldName != "Submit" && !y.GuiInfo.Disabled).ForEach(y => y.Disabled = true);
            }
            else
            {
                listViewItem.FilterChildren(y => !y.GuiInfo.Disabled).ForEach(y => y.Disabled = false);
            }
        }

        public async Task AddOrUpdateBookingList()
        {
            await this.OpenPopup(
                featureName: "Ship Book Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.BookingListEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Cập nhật lịch book tàu";
                    instance.Entity = new BookingList();
                    return instance;
                });
        }

        public async Task CreateAllBookingList()
        {
            await new Client(nameof(BookingList)).PostAsync<BookingList>(null, "CreateAllBookingList");
        }

        private int awaiter;

        public void CalcTotalPriceAndTotalFee(BookingList bookingList)
        {
            Window.ClearTimeout(awaiter);
            awaiter = Window.SetTimeout(async () =>
            {
                await CalcTotalPriceAndTotalFeeAsync(bookingList);
            }, 500);
        }

        public async Task CalcTotalPriceAndTotalFeeAsync(BookingList bookingList)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            if (gridView is null)
            {
                return;
            }
            var listViewItem = gridView.GetListViewItems(bookingList).FirstOrDefault();
            if (listViewItem != null)
            {
                bookingList.TotalPrice = bookingList.ShipUnitPrice * bookingList.Count;
                bookingList.TotalFee = bookingList.TotalPrice + bookingList.OrtherFeePrice;
                var res = await new Client(nameof(BookingList)).PatchAsync<BookingList>(GetPatchEntity(bookingList));
                if (res != null)
                {
                    await gridView.ApplyFilter(true);
                    gridView.Dirty = false;
                }
            }
        }

        public async Task LockAllBookingList()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(BookingList));
            if (gridView is null)
            {
                return;
            }
            var listViewItems = (await gridView.GetRealTimeSelectedRows()).Cast<BookingList>().Where(x => x.Submit == false).ToList();
            if (listViewItems.Count() <= 0)
            {
                listViewItems = gridView.RowData.Data.Cast<BookingList>().Where(x => x.Submit == false).ToList();
            }
            if (listViewItems.Count <= 0)
            {
                Toast.Warning("Không có danh sách book tàu nào cần khóa");
                return;
            }
            var confirm = new ConfirmDialog
            {
                Content = "Bạn có chắc chắn muốn khóa " + listViewItems.Count() + " danh sách book tàu không ?",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                foreach (var item in listViewItems)
                {
                    item.Submit = true;
                    var listViewItem = gridView.GetListViewItems(item).FirstOrDefault();
                    await new Client(nameof(BookingList)).PatchAsync<BookingList>(GetPatchSubmit(item));
                    listViewItem.FilterChildren(y => y.GuiInfo.FieldName != "Submit" && !y.GuiInfo.Disabled).ForEach(y => y.Disabled = true);
                }
                await gridView.ApplyFilter(true);
                gridView.Dirty = false;
            };
        }

        public async Task UnLockAllBookingList()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(BookingList));
            if (gridView is null)
            {
                return;
            }
            var listViewItems = (await gridView.GetRealTimeSelectedRows()).Cast<BookingList>().Where(x => x.Submit).ToList();
            if (listViewItems.Count() <= 0)
            {
                listViewItems = gridView.RowData.Data.Cast<BookingList>().Where(x => x.Submit).ToList();
            }
            if (listViewItems.Count <= 0)
            {
                Toast.Warning("Không có danh sách book tàu nào cần mở khóa");
                return;
            }
            var confirm = new ConfirmDialog
            {
                Content = "Bạn có chắc chắn muốn mở khóa " + listViewItems.Count() + " danh sách book tàu không ?",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                foreach (var item in listViewItems)
                {
                    item.Submit = false;
                    var listViewItem = gridView.GetListViewItems(item).FirstOrDefault();
                    await new Client(nameof(BookingList)).PatchAsync<BookingList>(GetPatchSubmit(item));
                    listViewItem.FilterChildren(y => !y.GuiInfo.Disabled).ForEach(y => y.Disabled = false);
                }
                await gridView.ApplyFilter(true);
                gridView.Dirty = false;
            };
        }

        public async Task ApproveUnLockShip()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationUnLockShip");
            if (gridView == null)
            {
                return;
            }
            var transportations = (await gridView.GetRealTimeSelectedRows()).Cast<Transportation>().Where(x => x.LockShip).ToList();
            if (transportations.Count <= 0)
            {
                Toast.Warning("Không có cont nào bị khóa !!!");
                return;
            }
            var confirm = new ConfirmDialog
            {
                Content = $"Bạn có chắc chắn muốn mở khóa cho {transportations.Count} DSVC không?",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                foreach (var item in transportations)
                {
                    item.LockShip = false;
                    item.IsRequestUnLockShip = false;
                }
                var res = await new Client(nameof(Transportation)).PostAsync<bool>(transportations, "ApproveUnLockShip");
                if (res)
                {
                    gridView.RemoveRange(transportations);
                    Toast.Success("Mở khóa thành công");
                }
                else
                {
                    Toast.Warning("Đã có lỗi xảy ra");
                }
            };
        }

        public PatchUpdate GetPatchEntity(BookingList bookingList)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = bookingList.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(BookingList.ShipUnitPrice), Value = bookingList.ShipUnitPrice.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(BookingList.Count), Value = bookingList.Count.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(BookingList.TotalPrice), Value = bookingList.TotalPrice.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(BookingList.OrtherFeePrice), Value = bookingList.OrtherFeePrice.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(BookingList.TotalFee), Value = bookingList.TotalFee.ToString() });
            return new PatchUpdate { Changes = details };
        }

        public PatchUpdate GetPatchSubmit(BookingList bookingList)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = bookingList.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(BookingList.Submit), Value = bookingList.Submit.ToString() });
            return new PatchUpdate { Changes = details };
        }
    }
}
