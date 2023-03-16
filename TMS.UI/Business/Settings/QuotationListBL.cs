using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using System;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class QuotationListBL : TabEditor
    {
        public QuotationListBL() : base(nameof(Quotation))
        {
            Name = "Quotation List";
        }

        public async Task EditQuotationRegion(Quotation entity)
        {
            var parent = new Quotation();
            if (entity.ParentId != null)
            {
                parent = await new Client(nameof(Quotation)).FirstOrDefaultAsync<Quotation>($"?$filter=Active eq true and Id eq {entity.ParentId.Value}");
            }
            else
            {
                parent = entity;
            }
            await this.OpenPopup(
                featureName: "Quotation Region Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.QuotationRegionEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa bảng giá";
                    instance.Entity = parent;
                    return instance;
                });
        }

        public async Task AddQuotationRegion()
        {
            await this.OpenPopup(
                featureName: "Quotation Region Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.QuotationRegionEditorBL");
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