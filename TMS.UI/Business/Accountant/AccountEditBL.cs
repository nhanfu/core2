using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
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
    public class AccountEditBL : PopupEditor
    {
        public MasterData masterDataEntity => Entity as MasterData;
        public AccountEditBL() : base(nameof(MasterData))
        {
            Name = "Account Editor";
        }

        public void CheckParentId(MasterData masterData)
        {
            if (masterData.ParentId == 23991)
            {
                this.SetDisabled(true, "ParentId");
            }
        }

        public void BeforeMasterData(MasterData masterData)
        {
            masterData.ParentId = masterDataEntity.Id;
            masterData.Path = @"\23991\" + masterDataEntity.Id + @"\";
            masterData.Level = 2;
        }
    }
}
