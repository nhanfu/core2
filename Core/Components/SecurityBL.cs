using Core.Models;
using Core.ViewModels;
using Core.Components.Extensions;
using Core.Components.Forms;

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
    }
}