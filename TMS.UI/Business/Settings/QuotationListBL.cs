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

namespace TMS.UI.Business.Settings
{
    public class QuotationListBL : TabEditor
    {
        private HTMLInputElement _uploaderQuotation;
        private HTMLInputElement _uploaderQuotationAdjustment;
        private HTMLInputElement _uploaderQuotationCVC;
        private HTMLInputElement _uploaderQuotationCVCReturns;
        private HTMLInputElement _uploaderQuotationLiftLlowerGoods;
        private HTMLInputElement _uploaderQuotationLiftLlowerHollow;
        private HTMLInputElement _uploaderQuotationCombination;
        private Quotation CurrentQuotation;
        private GridView CurrentGridView;
        public QuotationListBL() : base(nameof(Quotation))
        {
            Name = "Quotation List";
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcelQuotation(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploaderQuotation = Html.Context as HTMLInputElement;
            };
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcelQuotationAdjustment(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploaderQuotationAdjustment = Html.Context as HTMLInputElement;
            };
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcelQuotationCVCReturns(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploaderQuotationCVCReturns = Html.Context as HTMLInputElement;
            };
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcelQuotationCVC(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploaderQuotationCVC = Html.Context as HTMLInputElement;
            };
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcelQuotationLiftLlowerGoods(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploaderQuotationLiftLlowerGoods = Html.Context as HTMLInputElement;
            };
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcelQuotationLiftLlowerHollow(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploaderQuotationLiftLlowerHollow = Html.Context as HTMLInputElement;
            };
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcelQuotationCombination(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploaderQuotationCombination = Html.Context as HTMLInputElement;
            };
        }

        public async Task EditQuotation(Quotation entity)
        {
            await this.OpenPopup(
                featureName: "Quotation Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.QuotationEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa bảng giá";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public async Task AddQuotation()
        {
            await this.OpenPopup(
                featureName: "Quotation Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.QuotationEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới bảng giá";
                    instance.Entity = new Quotation();
                    return instance;
                });
        }

        public void BeforeCreatedTab1(Quotation quotation)
        {
            // CVC (Đóng hàng)
            quotation.TypeId = 7592;
        }

        public void BeforeCreatedTab2(Quotation quotation)
        {
            // CVC Trả hàng
            quotation.TypeId = 7593;
        }

        public void BeforeCreatedTab3(Quotation quotation)
        {
            // Nâng rỗng
            quotation.TypeId = 7594;
        }

        public void BeforeCreatedTab4(Quotation quotation)
        {
            // Hạ rỗng
            quotation.TypeId = 7595;
        }

        public void BeforeCreatedTab5(Quotation quotation)
        {
            // Nâng rỗng
            quotation.TypeId = 7596;
        }

        public void BeforeCreatedTab6(Quotation quotation)
        {
            // Hạ rỗng
            quotation.TypeId = 7597;
        }

        public void BeforeCreatedTab7(Quotation quotation)
        {
            // Cước tàu
            quotation.TypeId = 7598;
        }

        public void BeforeCreatedTab8(Quotation quotation)
        {
            // Điều chỉnh cước tàu
            quotation.TypeId = 11483;
        }

        public void BeforeCreatedTab9(Quotation quotation)
        {
            // Chi phí kết hợp
            quotation.TypeId = 12071;
        }

        private async Task SelectedExcelQuotation(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploaderQuotation.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<Quotation>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportQuotation",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportQuotation()
        {
            _uploaderQuotation.Click();
        }

        private async Task SelectedExcelQuotationAdjustment(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploaderQuotationAdjustment.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<Quotation>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportQuotationAdjustment",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportQuotationAdjustment()
        {
            _uploaderQuotationAdjustment.Click();
        }

        private async Task SelectedExcelQuotationCVC(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploaderQuotationCVC.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<Quotation>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportQuotationCVC",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportQuotationCVC()
        {
            _uploaderQuotationCVC.Click();
        }

        private async Task SelectedExcelQuotationCVCReturns(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploaderQuotationCVCReturns.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<Quotation>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportQuotationCVCReturns",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportQuotationCVCReturns()
        {
            _uploaderQuotationCVCReturns.Click();
        }

        private async Task SelectedExcelQuotationLiftLlowerGoods(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploaderQuotationLiftLlowerGoods.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<Quotation>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportQuotationLiftLlowerGoods",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportQuotationLiftLlowerGoods()
        {
            _uploaderQuotationLiftLlowerGoods.Click();
        }

        private async Task SelectedExcelQuotationLiftLlowerHollow(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploaderQuotationLiftLlowerHollow.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<Quotation>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportQuotationLiftLlowerHollow",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportQuotationLiftLlowerHollow()
        {
            _uploaderQuotationLiftLlowerHollow.Click();
        }

        private async Task SelectedExcelQuotationCombination(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploaderQuotationCombination.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<Quotation>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportQuotationCombination",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportQuotationCombination()
        {
            _uploaderQuotationCombination.Click();
        }

        public async Task UpdateQuotation()
        {
            
            await this.OpenPopup(
                featureName: "Quotation Update Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.QuotationUpdateEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Điều chỉnh báo giá";
                    instance.Entity = new QuotationUpdate();
                    return instance;
                });
        }
    }
}