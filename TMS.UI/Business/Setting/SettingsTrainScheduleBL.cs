using System;
using System.Threading.Tasks;
using TMS.API.Models;
using Core.Components.Extensions;
using Core.Components.Forms;

namespace TMS.UI.Business.Setting
{
    public class SettingsTrainScheduleBL : TabEditor
    {
        public SettingsTrainScheduleBL() : base(nameof(MasterData))
        {
            Name = "Settings Train Schedule";
        }

        public void CreateSettingsETAHPH(MasterData masterData)
        {
            masterData.Level = 2;
            masterData.ParentId = 25219;
            masterData.Path = @"\25218\25219\";
        }

        public void CreateSettingsETADAN(MasterData masterData)
        {
            masterData.Level = 2;
            masterData.ParentId = 25220;
            masterData.Path = @"\25218\25220\";
        }

        public void CreateSettingsETACLO(MasterData masterData)
        {
            masterData.Level = 2;
            masterData.ParentId = 25221;
            masterData.Path = @"\25218\25221\";
        }

        public void CreateSettingsREMARK(MasterData masterData)
        {
            masterData.Level = 2;
            masterData.ParentId = 25222;
            masterData.Path = @"\25218\25222\";
        }
    }
}
