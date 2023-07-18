using Core.Models;
using Core.ViewModels;
using Core.Clients;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using System.Threading.Tasks;

namespace Core.Components
{
    public class HeaderEditor : PopupEditor
    {
        private SyncConfigVM _syncConfig;
        public HeaderEditor() : base(nameof(GridPolicy))
        {
            Name = "HeaderEditor";
            Title = "Properties";
            Icon = "fa fa-wrench";
            DOMContentLoaded += AlterPosition;
            PopulateDirty = false;
            Config = true;
            ShouldLoadEntity = true;
        }

        private void AlterPosition()
        {
            Element.ParentElement.AddClass("properties");
        }

        public async Task<bool> SyncConfig()
        {
            var component = Entity as GridPolicy;
            if (Dirty)
            {
                await Save();
            }
            var header = Entity as GridPolicy;
            header.Feature = (Parent as EditForm).Feature;
            _syncConfig = new SyncConfigVM
            {
                GridPolicy = header
            };
            var syncDialog = new ConfirmDialog()
            {
                Entity = _syncConfig,
                Content = "Chọn danh sách khách hàng cần đồng bộ",
            };
            AddChild(syncDialog);
            var vendorSearch = new MultipleSearchEntry(new Component
            {
                PlainText = "Cập nhật cho các công ty",
                ShowLabel = true,
                Label = "Công ty",
                FieldName = nameof(SyncConfigVM.VendorId),
                FormatData = "{CompanyLocalShortName}",
                Row = 12,
                ReferenceId = Utils.GetEntity(nameof(Vendor))?.Id ?? 0,
                DataSourceFilter = "?$filter=Active eq true and IsTenant eq true",
                Visibility = true,
            });
            syncDialog.AddChild(vendorSearch);
            syncDialog.DisposeAfterYes = false;
            syncDialog.YesConfirmed += () => Task.Run(SyncDialog_YesConfirmed);
            return true;
        }

        private async Task SyncDialog_YesConfirmed()
        {
            _syncConfig.GridPolicy.ClearReferences();
            var ok = await new Client(nameof(Component), typeof(User).Namespace).PostAsync<bool>(_syncConfig, "SyncTenant", allowNested: true);
            if (ok)
            {
                Toast.Success("Cập nhật cấu hình thành công");
            }
        }
    }
}