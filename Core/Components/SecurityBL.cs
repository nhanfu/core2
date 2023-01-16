using Core.Models;
using Core.ViewModels;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using System.Threading.Tasks;

namespace Core.Components
{
    public class SecurityBL : PopupEditor
    {
        private SecurityVM Security => Entity as SecurityVM;
        public SecurityBL() : base(nameof(FeaturePolicy))
        {
            Name = "SecurityEditor";
            Title = "Bảo mật & Phân quyền";
            Icon = "mif-security";
        }
    }

    public class SecurityEditorBL : PopupEditor
    {
        private SecurityVM SecurityEntity => Entity as SecurityVM;
        public SecurityEditorBL() : base(nameof(FeaturePolicy))
        {
            Name = "CreateSecurity";
            Title = "Bảo mật & Phân quyền";
            Icon = "mif-security";
            DOMContentLoaded += CheckAllPolicy;
        }

        public void CheckAllPolicy()
        {
            SecurityEntity.CanDelete = SecurityEntity.AllPermission;
            SecurityEntity.CanDeactivate = SecurityEntity.AllPermission;
            SecurityEntity.CanRead = SecurityEntity.AllPermission;
            SecurityEntity.CanWrite = SecurityEntity.AllPermission;
            SecurityEntity.CanShare = SecurityEntity.AllPermission;
            this.FindComponentByName<Section>("Properties").UpdateView();
        }

        public void CheckPolicy()
        {
            SecurityEntity.AllPermission = !(!SecurityEntity.CanDeactivate || !SecurityEntity.CanDelete || !SecurityEntity.CanRead || !SecurityEntity.CanShare || !SecurityEntity.CanWrite);
            this.FindComponentByName<Section>("Properties").UpdateView();
        }

        public override async Task<bool> Save(object entity)
        {
            if (SecurityEntity.UserId is null && SecurityEntity.RoleId is null)
            {
                Toast.Warning("User và Role không thể đồng thời để trống");
                return false;
            }
            if (SecurityEntity.UserId != null && SecurityEntity.RoleId != null)
            {
                Toast.Warning("User và Role không thể đồng thời có giá trị");
                return false;
            }
            var res = await Client.PostAsync<bool>(SecurityEntity, "SharePermission");
            if (res)
            {
                Toast.Success("Đã tạo mới thành công");
            }
            else
            {
                Toast.Warning("Chia sẻ quyền hạn không thành công");
            }

            SecurityEntity.RoleId = null;
            SecurityEntity.UserId = null;
            Parent.UpdateView();
            Dirty = false;
            return true;
        }
    }
}