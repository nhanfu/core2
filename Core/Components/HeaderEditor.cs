using Core.Clients;
using Core.Components.Forms;
using Core.Extensions;
using Core.Models;
using Core.ViewModels;
using System.Threading.Tasks;

namespace Core.Components
{
    public class HeaderEditor : PopupEditor
    {
        private SyncConfigVM _syncConfig;
        public HeaderEditor() : base(nameof(Component))
        {
            Name = "ComponentEditor";
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

        private async Task SyncDialog_YesConfirmed()
        {
            var ok = await new Client(nameof(Component), typeof(User).Namespace).PostAsync<bool>(_syncConfig, "SyncTenant", allowNested: true);
            if (ok)
            {
                Toast.Success("Cập nhật cấu hình thành công");
            }
        }
    }
}