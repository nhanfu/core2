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
    public class BookingListBL : TabEditor
    {
        private HTMLInputElement _uploader;
        public bool _isLoadTransportation { get; set; }
        private HTMLInputElement _uploaderTeus;
        public GridView gridView;
        public BookingListBL() : base(nameof(Booking))
        {
            Name = "Booking List";
            DOMContentLoaded += () =>
            {
                NotificationClient?.AddListener(Utils.GetEntity(nameof(Teus)).Id, RealtimeUpdate);
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcel(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploader = Html.Context as HTMLInputElement;
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcelTeus(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploaderTeus = Html.Context as HTMLInputElement;
            };
        }

        public async Task LockBooking()
        {
            gridView = this.FindComponentByName<GridView>(nameof(Booking));
            var lockUpdate = gridView.RowData.Data.Cast<Booking>().Where(x => x.BookingExpired != null && x.BookingExpired < DateTime.Now).ToList();
            gridView.FilterChildren(x => x.Entity != null && lockUpdate.Contains(x.Entity)).ForEach(x => x.SetDisabled(true));
            gridView.BodyContextMenuShow += () =>
            {
                ContextMenu.Instance.MenuItems = new List<ContextMenuItem>
                {
                        new ContextMenuItem { Icon = "fal fa-street-view", Text = "Xem danh sách vận chuyển", Click = ViewTransportation },
                        new ContextMenuItem { Icon = "fal fa-binoculars", Text = "Chọn booking", Click = ChooseBooking },
                };
            };
        }

        private void ChooseBooking(object arg)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Booking));
            Task.Run(async () =>
            {
                var selected = await gridView.GetRealTimeSelectedRows();
                if (selected.Nothing())
                {
                    selected = gridView.RowData.Data;
                }
                var coords = selected.Cast<Booking>().FirstOrDefault();
                var fe = Tabs.Where(x => x.Name == "Transportation List").FirstOrDefault();
                if (fe is null)
                {
                    Toast.Warning("Vui lòng mở màn hình danh sách vận chuyển");
                    return;
                }
                var gridView1 = fe.FilterChildren<GridView>().FirstOrDefault(x => x.GuiInfo.Id == 16016);
                if (gridView1 is null)
                {
                    return;
                }
                fe.Focus();
                var rowData = (await gridView1.GetRealTimeSelectedRows()).Cast<Transportation>().ToList();
                if (rowData.Nothing())
                {
                    Toast.Warning("Vui lòng chọn danh sách vận chuyển");
                    return;
                }
                var items = gridView1.GetListViewItems(rowData);
                foreach (var updated in items)
                {
                    updated.Entity.SetComplexPropValue(nameof(Transportation.BookingId), coords.Id);
                    updated.UpdateView();
                    var dropdown = updated.FilterChildren<SearchEntry>(x => x.GuiInfo.FieldName == nameof(Transportation.BookingId)).FirstOrDefault();
                    updated.PopulateFields(dropdown.Matched);
                    await updated.DispatchEventToHandlerAsync(updated.GuiInfo.Events, EventType.Change, updated.Entity, dropdown.Matched);
                    updated.PatchUpdate();
                }
                Toast.Success("Chọn booking thành công");
            });
        }

        private void ViewTransportation(object arg)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Booking));

            Task.Run(async () =>
            {
                var selected = await gridView.GetRealTimeSelectedRows();
                if (selected.Nothing())
                {
                    selected = gridView.RowData.Data;
                }
                var coords = selected.Cast<Booking>().ToList().Select(x => x.Id).Distinct().ToList();
                var fe = Tabs.Where(x => x.Name == "Transportation List").FirstOrDefault();
                if (fe is null)
                {
                    var currentFeature = await ComponentExt.LoadFeatureByName("Transportation List");
                    var id = currentFeature.Name + currentFeature.Id;
                    Type type;
                    if (currentFeature.ViewClass != null)
                    {
                        type = Type.GetType(currentFeature.ViewClass);
                    }
                    else
                    {
                        type = typeof(TabEditor);
                    }
                    fe = Activator.CreateInstance(type) as TabEditor;
                    fe.Name = currentFeature.Name;
                    fe.Id = id;
                    fe.Icon = currentFeature.Icon;
                    fe.Feature = currentFeature;
                    fe.Render();
                    fe.DOMContentLoaded += () =>
                    {
                        var gridView1 = fe.FilterChildren<GridView>().FirstOrDefault(x => x.GuiInfo.Id == 16016);
                        gridView1.DOMContentLoaded += async () =>
                        {
                            if (_isLoadTransportation)
                            {
                                return;
                            }
                            await FilterTransportation(coords, fe);
                            _isLoadTransportation = true;
                        };
                    };
                }
                else
                {
                    await FilterTransportation(coords, fe);
                    fe.Focus();
                }
            });
        }

        private async Task FilterTransportation(List<int> coords, TabEditor fe)
        {
            var gridView1 = fe.FilterChildren<GridView>().FirstOrDefault(x => x.GuiInfo.Id == 16016);
            if (gridView1 is null)
            {
                return;
            }
            gridView1.CellSelected.Clear();
            gridView1.AdvSearchVM.Conditions.Clear();
            coords.ForEach(x =>
            {
                gridView1.CellSelected.Add(new Core.Models.CellSelected
                {
                    FieldName = "BookingId",
                    FieldText = "Booking",
                    ComponentType = "Dropdown",
                    Value = x.ToString(),
                    ValueText = x.ToString(),
                    Operator = "in",
                    OperatorText = "Chứa",
                    IsSearch = true,
                    Logic = LogicOperation.Or
                });
            });
            await gridView1.ActionFilter();
        }

        public void CalcTeusBooking(Booking booking)
        {
            gridView = this.FindComponentByName<GridView>(nameof(Booking));
            var listViewItem = gridView.GetListViewItems(booking).FirstOrDefault();
            if (booking.Teus20Using > booking.Teus20)
            {
                Toast.Warning("Số teus20 đóng không được lớn hơn số teus cấp");
                booking.Teus20Using = booking.Teus20;
            }
            if (Convert.ToDecimal(booking.Teus40Using) > Convert.ToDecimal(booking.Teus40))
            {
                Toast.Warning("Số teus40 đóng không được lớn hơn số teus cấp");
                booking.Teus40Using = booking.Teus40;
            }
            booking.Teus20Remain = booking.Teus20 - booking.Teus20Using;
            booking.Teus40Remain = booking.Teus40 - booking.Teus40Using;
            listViewItem.UpdateView();
            var updated = listViewItem.FilterChildren<CellText>(x => x.GuiInfo.FieldName == nameof(Booking.Teus20Remain) || x.GuiInfo.FieldName == nameof(Booking.Teus40Remain)).ToList();
            updated.ForEach(x => x.Dirty = true);
        }

        public void CalcTeusSlot(Teus teus)
        {
            gridView = this.FindComponentByName<GridView>(nameof(Teus));
            if (teus.Teus20Using > teus.Teus20)
            {
                Toast.Warning("Số teus20 đóng không được lớn hơn số teus cấp");
                teus.Teus20Using = teus.Teus20;
            }
            if (Convert.ToDecimal(teus.Teus40Using) > Convert.ToDecimal(teus.Teus40))
            {
                Toast.Warning("Số teus40 đóng không được lớn hơn số teus cấp");
                teus.Teus40Using = teus.Teus40;
            }
            var listViewItem = gridView.GetListViewItems(teus).FirstOrDefault();
            teus.Teus20Remain = teus.Teus20 - teus.Teus20Using;
            teus.Teus40Remain = teus.Teus40 - teus.Teus40Using;
            listViewItem.UpdateView();
            var updated = listViewItem.FilterChildren<CellText>(x => x.GuiInfo.FieldName == nameof(Teus.Teus20Remain) || x.GuiInfo.FieldName == nameof(Teus.Teus40Remain)).ToList();
            updated.ForEach(x => x.Dirty = true);
        }

        public async Task CheckUnique(Booking booking)
        {
            gridView = this.FindComponentByName<GridView>(nameof(Booking));
            if (booking.Id > 0)
            {
                var check = await new Client(nameof(Booking)).FirstOrDefaultAsync<Booking>($"?$filter=Active eq true and ShipId eq {booking.ShipId} and Trip eq '{booking.Trip}' and StartShip eq {booking.StartShip.Value.ToOdataFormat()} and Id ne {booking.Id} and BookingNo eq '{booking.BookingNo}'");
                if (check != null)
                {
                    Toast.Warning("Đã tồn tại booking trong hệ thống");
                    return;
                }
            }
            else
            {
                var check = await new Client(nameof(Booking)).FirstOrDefaultAsync<Booking>($"?$filter=Active eq true and ShipId eq {booking.ShipId} and Trip eq '{booking.Trip}' and StartShip eq {booking.StartShip.Value.ToOdataFormat()} and BookingNo eq '{booking.BookingNo}'");
                if (check != null)
                {
                    Toast.Warning("Đã tồn tại booking trong hệ thống");
                    return;
                }
            }
        }

        public async Task EditBooking(Booking entity)
        {
            await this.OpenPopup(
                featureName: "Booking Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.BookingEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa booking";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public async Task AddBooking()
        {
            await this.OpenPopup(
                featureName: "Booking Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.BookingEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới booking";
                    instance.Entity = new Booking();
                    return instance;
                });
        }

        public override async Task<bool> BulkUpdate()
        {
            var rs = await base.BulkUpdate();
            await LockBooking();
            return rs;
        }

        public void ViewBooking()
        {
            var gridView = this.FindComponentByName<GridView>(nameof(Teus));
            gridView.AllListViewItem.ToList().ForEach(x =>
            {
                var teus = x.Entity.Cast<Teus>();
                x.Element.RemoveClass("bg-host");
                if (teus.Teus20Remain < 0 || teus.Teus40Remain < 0)
                {
                    x.Element.AddClass("bg-host");
                }
            });
        }


        private async Task SelectedExcel(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }
            var uploadForm = _uploader.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<Booking>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportExcel",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportExcel()
        {
            _uploader.Click();
        }

        private async Task SelectedExcelTeus(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }
            var uploadForm = _uploaderTeus.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<Teus>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportExcelTeus",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportExcelTeus()
        {
            _uploaderTeus.Click();
        }
    }
}
