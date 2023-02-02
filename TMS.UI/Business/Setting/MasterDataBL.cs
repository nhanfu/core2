using System;
using System.Threading.Tasks;
using TMS.API.Models;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Clients;
using Core.Extensions;
using Bridge.Html5;
using Core.MVVM;
using System.Collections.Generic;
using Core.Enums;

namespace TMS.UI.Business.Setting
{
    public class MasterDataBL : TabEditor
    {
        private HTMLInputElement _uploaderExpense;
        private HTMLInputElement _uploaderExpensePrice;
        private HTMLInputElement _uploaderCommodity;
        private HTMLInputElement _uploaderShipBrand;
        private HTMLInputElement _uploaderQuotation;
        public MasterDataBL() : base(nameof(MasterData))
        {
            Name = "Master Data";
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcelExpense(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploaderExpense = Html.Context as HTMLInputElement;
            };
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcelExpensePrice(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploaderExpensePrice = Html.Context as HTMLInputElement;
            };
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcelCommodity(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploaderCommodity = Html.Context as HTMLInputElement;
            };
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcelShipBrand(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploaderShipBrand = Html.Context as HTMLInputElement;
            };
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcelQuotationType(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploaderQuotation = Html.Context as HTMLInputElement;
            };
        }

        public async Task EditMasterData(MasterData masterData)
        {
            await this.OpenPopup(
                featureName: "MasterData Detail",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Setting.MasterDataDetailsBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Update tham chiếu";
                    instance.Entity = masterData ?? new MasterData();
                    return instance;
                });
        }

        public async Task CreateMasterData()
        {
            await this.OpenPopup(
                featureName: "MasterData Detail",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Setting.MasterDataDetailsBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm tham chiếu mới";
                    instance.Entity = new MasterData();
                    return instance;
                });
        }

        public async Task UpdatePath()
        {
            await new Client(nameof(MasterData)).PostAsync<bool>(null, $"UpdatePath");
        }

        public async Task EditMasterDataParent(MasterData parent)
        {
            var masterData = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Id eq {parent.ParentId}");
            await this.OpenPopup(
                featureName: "MasterData Detail",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Setting.MasterDataDetailsBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Update tham chiếu";
                    instance.Entity = masterData;
                    return instance;
                });
        }

        private async Task SelectedExcelExpense(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploaderExpense.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<MasterData>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportExpenseType",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportExpenseType()
        {
            _uploaderExpense.Click();
        }

        private async Task SelectedExcelExpensePrice(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploaderExpensePrice.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<MasterData>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportExpensePrice",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportExpensePrice()
        {
            _uploaderExpensePrice.Click();
        }

        private async Task SelectedExcelCommodity(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploaderCommodity.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<MasterData>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportCommodityType",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportCommodityType()
        {
            _uploaderCommodity.Click();
        }

        private async Task SelectedExcelShipBrand(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploaderShipBrand.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<Vendor>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportShipBrand",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportShipBrand()
        {
            _uploaderShipBrand.Click();
        }

        private async Task SelectedExcelQuotationType(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploaderQuotation.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<MasterData>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportQuotationType",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportQuotationType()
        {
            _uploaderQuotation.Click();
        }
    }
}
