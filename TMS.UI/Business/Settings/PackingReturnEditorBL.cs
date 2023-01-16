using Core.Components;
using Core.Components.Forms;
using TMS.API.Models;
using Core.Enums;
using Core.Extensions;
using System.Collections.Generic;
using System.Linq;
using Core.Components.Extensions;

namespace TMS.UI.Business.Settings
{
    public class PackingReturnEditorBL : PopupEditor
    {
        public Vendor VendorEntity => Entity as Vendor;
        public GridView gridView;
        
        public PackingReturnEditorBL() : base(nameof(Vendor))
        {
            Name = "Packing Return Editor";
        }

        public void Check_PackingReturn(VendorService vendorService, MasterData masterData)
        {
            gridView = gridView ?? this.FindActiveComponent<GridView>().FirstOrDefault();

            if (VendorEntity.VendorService.Any(x => x.Id != vendorService.Id && x.ServiceId == masterData.Id))
            {
                Toast.Warning("Đơn vị này đã được chọn !!!");
                gridView.RemoveRow(vendorService);
            }
        }
    }
}