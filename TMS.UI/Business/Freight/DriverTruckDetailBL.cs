using Bridge.Html5;
using Core.API.ViewModels;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Enums;
using TMS.API.Models;

namespace TMS.UI.Business.Freight
{
    public class DriverTruckDetailBL : BaseDriverDetailBL
    {
        public DriverTruckDetailBL() : base()
        {
            Name = "CoordinationTransit Truck";
            Title = "Xem chi tiết vận chuyển";
            ShouldUpdateParentForm = true;
            DOMContentLoaded += () =>
            {
                Truck = CoorDEntity.Truck;
                _attachments = this.FindComponentByName<ImageUploader>("Attachments");
                DisableSection();
                ChangeButtonStatus(true);
            };
            CurrentGps = JsonConvert.DeserializeObject<List<GPS>>(Window.LocalStorage.GetItem("coordinations")?.ToString())?.LastOrDefault();
        }

        public void ActionSaveStatus()
        {
            _attachments?.OpenFileDialog(Window.Instance["event"].As<Event>());
        }

        public async Task AddSurchargePayable()
        {
            await this.OpenTab("SurchargePayable Driver" + base.GetHashCode(), "SurchargePayable Driver", (Func<TabEditor>)(() =>
            {
                var form = Activator.CreateInstance(Type.GetType("TMS.UI.Business.Sale.SurchargePayableDriverBL")) as TabEditor;
                form.Entity = new SurchargePayable()
                {
                    ExchangeRate = 1,
                    CurrencyId = (int)CurrencyEnum.VND,
                    UserId = CoorDEntity.DriverId,
                    Vat = 0,
                    Quantity = 1,
                    CoordinationDetailId = CoorDEntity.Id,
                    OrderDetailId = CoorDEntity.OrderDetailId,
                    InvoiceDate = DateTime.Now,
                    EntityId = (int)EntityEnum.PaybackPayment,
                    TruckId = CoorDEntity.TruckId,
                    StatusId = (int)ApprovalStatusEnum.New,
                };
                form.Title = "Thêm mới chi phí";
                return form;
            }));
        }

        public async void EditSurchargePayable(SurchargePayable surchargePayable)
        {
            await this.OpenTab("SurchargePayable Driver" + surchargePayable.Id, "SurchargePayable Driver", () =>
            {
                var form = Activator.CreateInstance(Type.GetType("TMS.UI.Business.Sale.SurchargePayableDriverBL")) as TabEditor;
                form.Entity = surchargePayable;
                form.Title = "Chỉnh sửa chi phí";
                return form;
            });
        }

        public async Task CreatePaybackPayment()
        {
            await this.OpenPopup(featureName: "AddOrEditSurchargePayable", factory: () =>
            {
                var form = Activator.CreateInstance(Type.GetType("TMS.UI.Business.Freight.PaybackPaymentDetailBL")) as PopupEditor;
                form.Entity = new PaybackPayment();
                return form;
            });
        }

        public void ChangeIn()
        {
            var orderDetailGrid = this.FindActiveComponent<ListView>().FirstOrDefault();
            var btnCreate = this.FindComponentByName<Button>("btnCreate");
            var btnSave = this.FindComponentByName<Button>("btnSave");
            btnCreate.Show = false;
            btnSave.Show = false;
            switch (orderDetailGrid.Name)
            {
                case nameof(CoordinationDetailBlock):
                    btnSave.Show = true;
                    break;
                case nameof(SurchargePayable):
                    btnCreate.Show = true;
                    break;
                default:
                    break;
            }
        }
    }
}
