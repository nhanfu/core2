using Core.Components.Extensions;
using Core.Components.Forms;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Models;

namespace Core.Fw.Authentication
{
    public class PolicyManagementBL : TabEditor
    {
        public PolicyManagementBL() : base(nameof(Role))
        {
            Name = "Policy";
            Title = Name;
        }

        public void CopyRole(List<Role> originRole, List<Role> copied)
        {
            copied.ForEach(x => x.InverseParentRole = null);
        }

        public async Task EditRole(Role role)
        {
            await this.OpenPopup(
               featureName: "Role Detail",
               factory: () =>
               {
                   var type = Type.GetType("Core.Fw.Setting.RoleDetailBL");
                   var instance = Activator.CreateInstance(type) as PopupEditor;
                   instance.Entity = role ?? new Role();
                   return instance;
               });
        }
    }
}
