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
        public BookingList BookingListEntity => Entity as BookingList;
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

        public async Task ReportDataByFilter()
        {
            var query = $"?$filter=Active eq true";
            var prequery = $"Active = 1";
            if (BookingListEntity.Month != null)
            {
                query += $" and Month eq {BookingListEntity.Month}";
                prequery += $" and Month = {BookingListEntity.Month}";
            }
            if (BookingListEntity.Year != null)
            {
                query += $" and Year eq {BookingListEntity.Year}";
                prequery += $" and Year = {BookingListEntity.Year}";
            }
            if (BookingListEntity.FromDate != null)
            {
                var fromDate = BookingListEntity.FromDate.Value.Date.ToString("yyyy-MM-dd");
                query += $" and StartShip ge {fromDate}";
                prequery += $" and StartShip >= '{fromDate}'";
            }
            if (BookingListEntity.ToDate != null)
            {
                var toDate = BookingListEntity.ToDate.Value.Date.ToString("yyyy-MM-dd");
                query += $" and StartShip le {toDate}";
                prequery += $" and StartShip <= '{toDate}'";
            }
            if (BookingListEntity.RouteId != null)
            {
                query += $" and RouteId eq {BookingListEntity.RouteId}";
                prequery += $" and RouteId = {BookingListEntity.RouteId}";
            }
            if (BookingListEntity.ContainerTypeId != null)
            {
                query += $" and ContainerTypeId eq {BookingListEntity.ContainerTypeId}";
                prequery += $" and ContainerTypeId = {BookingListEntity.ContainerTypeId}";
            }
            query += "&$orderby=StartShip desc";
            var bookingList = await new Client(nameof(BookingList)).GetRawList<BookingList>($"{query}");
            BookingListEntity.TotalCount = (decimal)bookingList.Sum(x => x.Count);
            BookingListEntity.TotalTotalPrice = (decimal)bookingList.Sum(x => x.TotalPrice);
            BookingListEntity.AVGTotalPrice = Math.Round(BookingListEntity.TotalTotalPrice / BookingListEntity.TotalCount);
            this.UpdateView(false, nameof(BookingList.TotalCount), nameof(BookingList.TotalTotalPrice), nameof(BookingList.AVGTotalPrice));
            var grid = this.FindActiveComponent<GridView>().FirstOrDefault();
            grid.DataSourceFilter = query;
            grid.GuiInfo.PreQuery = prequery;
            await grid.ActionFilter();
        }

        public async Task LoadReportDataByFilter()
        {
            var grid = this.FindComponentByName<GridView>("BookingListReport");
            grid.ClearRowData();
            var query = $"?$filter=Active eq true";
            if (BookingListEntity.Month != null)
            {
                query += $" and Month eq {BookingListEntity.Month}";
            }
            if (BookingListEntity.Year != null)
            {
                query += $" and Year eq {BookingListEntity.Year}";
            }
            if (BookingListEntity.FromDate != null)
            {
                var fromDate = BookingListEntity.FromDate.Value.Date.ToString("yyyy-MM-dd");
                query += $" and StartShip ge {fromDate}";
            }
            if (BookingListEntity.ToDate != null)
            {
                var toDate = BookingListEntity.ToDate.Value.Date.ToString("yyyy-MM-dd");
                query += $" and StartShip le {toDate}";
            }
            query += "&$orderby=StartShip desc";
            var bookingList = await new Client(nameof(BookingList)).GetRawList<BookingList>(query);
            var settingRoutes = await new Client(nameof(MasterData)).GetRawList<MasterData>($"?$filter=Active eq true and ParentId eq 26363");
            var routes = await new Client(nameof(Route)).GetRawList<Route>($"?$filter=Active eq true and Id in ({settingRoutes.Select(x => x.Enum).ToList().Combine()})");
            routes.ForEach(x => x.Order = (int)settingRoutes.Where(y => y.Enum == x.Id).FirstOrDefault().Order);
            var reportBookingList = new List<BookingList>();
            var containerTypes = await new Client(nameof(MasterData)).GetRawList<MasterData>($"?$filter=Active eq true and ParentId eq 7565");
            var containerTypesCont20 = containerTypes.Where(x => x.Description.Contains("20")).ToList();
            var containerTypesCont40 = containerTypes.Where(x => x.Description.Contains("40")).ToList();
            var containerTypeIdsCont20 = containerTypesCont20.Select(x => x.Id).ToList();
            var containerTypeIdsCont40 = containerTypesCont40.Select(x => x.Id).ToList();
            routes = routes.OrderBy(x => x.Order).ToList();
            foreach (var item in routes)
            {
                var filter = bookingList.Where(x => x.RouteId == item.Id).ToList();
                var newBookingList = new BookingList()
                {
                    RouteId = item.Id,
                    TotalCountCont20 = (decimal)filter.Where(x => containerTypeIdsCont20.Contains((int)x.ContainerTypeId)).Sum(x => x.Count),
                    TotalCountCont40 = (decimal)filter.Where(x => containerTypeIdsCont40.Contains((int)x.ContainerTypeId)).Sum(x => x.Count),
                    TotalTotalPriceCont20 = (decimal)filter.Where(x => containerTypeIdsCont20.Contains((int)x.ContainerTypeId)).Sum(x => x.TotalPrice),
                    TotalTotalPriceCont40 = (decimal)filter.Where(x => containerTypeIdsCont40.Contains((int)x.ContainerTypeId)).Sum(x => x.TotalPrice),
                };
                newBookingList.AVGTotalPriceCont20 = newBookingList.TotalTotalPriceCont20 != 0 && newBookingList.TotalCountCont20 != 0 ? Math.Round(newBookingList.TotalTotalPriceCont20 / newBookingList.TotalCountCont20) : 0;
                newBookingList.AVGTotalPriceCont40 = newBookingList.TotalTotalPriceCont40 != 0 && newBookingList.TotalCountCont40 != 0 ? Math.Round(newBookingList.TotalTotalPriceCont40 / newBookingList.TotalCountCont40) : 0;
                reportBookingList.Add(newBookingList);
                
            }
            await grid.AddRows(reportBookingList);
        }

        public void BeforeCreated(MasterData masterData)
        {
            masterData.Name = "TVC";
            masterData.Description = "TVC";
            masterData.Level = 1;
            masterData.ParentId = 26363;
            masterData.Path = @"\26363\";
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
