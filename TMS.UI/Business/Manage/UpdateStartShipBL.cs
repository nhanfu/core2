using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Manage
{
    public class UpdateStartShipBL : PopupEditor
    {
        public Transportation TransportationEntity => Entity as Transportation;
        public UpdateStartShipBL() : base(nameof(Transportation))
        {
            Name = "Update Start Ship";
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
            var transportations = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true " +
                $" and {nameof(Transportation.ShipId)} eq {TransportationEntity.ShipId}" +
                $" and {nameof(Transportation.Trip)} eq '{TransportationEntity.Trip}'" +
                $" and {nameof(Transportation.RouteId)} in ({TransportationEntity.RouteIds.Combine()})" +
                $" and cast(ShipDate,Edm.DateTimeOffset) eq {TransportationEntity.ShipDate?.ToOdataFormat()}" +
                $" and PortLiftId eq {(TransportationEntity.PortLiftId is null ? "null" : TransportationEntity.PortLiftId.ToString())}");
            if (transportations.Count == 0)
            {
                Toast.Warning("Không tìm thấy dữ liệu tương ứng");
                return;
            }
            var patchModel = transportations.Select(x => GetPathEntityUpdate(x)).ToList();
            await new Client(nameof(Transportation)).PatchAsync<object>(patchModel);
            Toast.Success($"Cập nhật ngày tàu cập thành công {transportations.Count} cont");
            Dirty = false;
            Dispose();
        }

        public async Task ChangeShip(Transportation transportation, Ship ship)
        {
            var trans = await new Client(nameof(Transportation)).FirstOrDefaultAsync<Transportation>($"?$filter=Active eq true and {nameof(Transportation.ShipId)} eq {TransportationEntity.ShipId}  and ShipDate ne null");
            TransportationEntity.Trip = trans.Trip;
            UpdateView(false, nameof(TransportationEntity.Trip));
        }

        private PatchUpdate GetPathEntity(Transportation transportation)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = transportation.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.ShipDate), Value = TransportationEntity.ShipDate?.Date.ToString("yyyy/MM/dd HH:mm:ss") });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.PortLiftId), Value = TransportationEntity.PortLiftId.ToString() });
            return new PatchUpdate { Changes = details };
        }

        private PatchUpdate GetPathEntityUpdate(Transportation transportation)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = transportation.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.ShipDate), Value = TransportationEntity.ShipDateNew?.Date.ToString("yyyy/MM/dd HH:mm:ss") });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.PortLiftId), Value = TransportationEntity.PortLiftNewId.ToString() });
            return new PatchUpdate { Changes = details };
        }
    }
}