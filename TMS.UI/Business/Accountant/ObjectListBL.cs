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
using TMS.API.ViewModels;

namespace TMS.UI.Business.Accountant
{
    public class ObjectListBL : TabEditor
    {
        private HTMLInputElement _uploader;
        public ObjectListBL() : base(nameof(Vendor))
        {
            Name = "Object List";
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcel(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploader = Html.Context as HTMLInputElement;
            };
        }

        public void GridViewDOMContentLoaded()
        {
            var grid = this.FindActiveComponent<GridView>().FirstOrDefault();
            if (grid != null)
            {
                grid.SetDisabled(true);
                SetDisabledGridView(grid);
            }
        }

        public void OpenEdit()
        {
            this.SetDisabled(false);
            this.SetShow(false, "btnEdit");
            var grid = this.FindActiveComponent<GridView>().FirstOrDefault();
            if (grid != null)
            {
                grid.AddNewEmptyRow();
                SetDefaultGridView(grid);
            }
        }

        public void SetDefaultGridView(GridView grid)
        {
            var listViewItems = grid.RowData.Data.Cast<Vendor>().ToList();
            listViewItems.ForEach(x =>
            {
                var listViewItem = grid.GetListViewItems(x).FirstOrDefault();
                if (listViewItem is null)
                {
                    return;
                }
                listViewItem.FilterChildren(y => y.GuiInfo.Active).ForEach(y => y.Disabled = true);
                listViewItem.FilterChildren(y => !y.GuiInfo.Disabled).ForEach(y => y.Disabled = false);
            });
        }

        public void SetDisabledGridView(GridView grid)
        {
            var listViewItems = grid.RowData.Data.Cast<Vendor>().ToList();
            listViewItems.ForEach(x =>
            {
                var listViewItem = grid.GetListViewItems(x).FirstOrDefault();
                if (listViewItem is null)
                {
                    return;
                }
                listViewItem.FilterChildren(y => y.GuiInfo.Active).ForEach(y => y.Disabled = true);
            });
        }

        public async Task AddObject()
        {
            await this.OpenPopup(
                featureName: "Object Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.ObjectEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới đối tượng công nợ";
                    instance.Entity = new Vendor()
                    {
                        TypeId = 23741
                    };
                    return instance;
                });
        }

        public async Task EditObject(Vendor vendor)
        {
            await this.OpenPopup(
                featureName: "Object Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.ObjectEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa đối tượng công nợ";
                    instance.Entity = vendor;
                    return instance;
                });
        }

        public void BeforeCreatedObject(Vendor vendor)
        {
            vendor.TypeId = 23741;
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
            var response = await Client.SubmitAsync<List<Vendor>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportObject",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportObject()
        {
            _uploader.Click();
        }

        public void MessageConfirmUpdate(Vendor entity, PatchUpdate patch)
        {
            var grid = this.FindActiveComponent<GridView>().FirstOrDefault();
            var confirm = new ConfirmDialog
            {
                Content = "Bạn có chắc chắn muốn cập nhật dữ liệu không?",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                entity.IsUpdate = true;
                patch.Changes.Add(new PatchUpdateDetail { Field = nameof(Vendor.IsUpdate), Value = entity.IsUpdate.ToString() });
                var res = await new Client(nameof(Vendor)).PatchAsync<Vendor>(patch);
            };
            confirm.NoConfirmed += async () =>
            {
                await grid.ApplyFilter(true);
            };
        }
    }
}
