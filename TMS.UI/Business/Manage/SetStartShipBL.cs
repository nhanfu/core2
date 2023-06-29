using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Manage
{
    public class SetStartShipBL : PopupEditor
    {
        public Transportation TransportationEntity => Entity as Transportation;
        public SetStartShipBL() : base(nameof(Transportation))
        {
            Name = "Set Start Ship";
        }

        public async Task ApplyChanges()
        {
            if (!await IsFormValid())
            {
                return;
            }
            if (TransportationEntity.RouteIds.Nothing())
            {
                Toast.Warning("Vui lòng chọn tuyến đường cần cập");
                return;
            }
            var transportations = await new Client(nameof(Transportation)).PostAsync<int>(TransportationEntity, $"SetStartShip");
            if (transportations > 0)
            {
                var listTran = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$select=Id&$filter=ShipId eq {TransportationEntity.ShipId} and (BrandShipId eq {(TransportationEntity.BrandShipId is null ? "null" : TransportationEntity.BrandShipId.ToString())} or {(TransportationEntity.BrandShipId is null ? "null" : TransportationEntity.BrandShipId.ToString())} eq {(TransportationEntity.BrandShipId is null ? "null" : TransportationEntity.BrandShipId.ToString())}) and Trip eq '{TransportationEntity.Trip}' and RouteId in ({TransportationEntity.RouteIds.Combine()})");
                foreach (var item in listTran)
                {
                    await new Client(nameof(History)).CreateAsync<History>(new History
                    {
                        ReasonOfChange = "Tàu cập",
                        TextHistory = $"Ngày tàu cập: {TransportationEntity.ShipDate.Value:dd/MM/yyyy}",
                        RecordId = item.Id,
                        EntityId = Utils.GetEntity(nameof(Transportation)).Id
                    });
                }
            }
            Toast.Success($"Cập nhật ngày tàu cập thành công {transportations} cont");
            Dirty = false;
            var gridTran = ParentForm.FindComponentByName<GridView>(nameof(Transportation));
            await gridTran?.ApplyFilter(true);
            Dispose();
        }

        public async Task ChangeShip(Transportation transportation, Ship ship)
        {
            var trans = await new Client(nameof(Transportation)).FirstOrDefaultAsync<Transportation>($"?$filter=Active eq true and {nameof(Transportation.ShipId)} eq {TransportationEntity.ShipId}  and ShipDate eq null");
            TransportationEntity.Trip = trans.Trip;
            UpdateView(false, nameof(TransportationEntity.Trip));
        }
    }
}