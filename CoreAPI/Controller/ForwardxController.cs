using Core.Extensions;
using Core.Services;
using CoreAPI.BgService;
using CoreAPI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Core.Controllers;

[Authorize]
public class ForwardxController(UserService _userSvc, IServiceProvider serviceProvider, IConfiguration configuration) : ControllerBase
{
    [HttpPost("api/Forwardx/GenTruckingShipment")]
    public async Task<string> GenTruckingShipment([FromBody] TruckingShipmentVM vm)
    {
        using (SqlConnection connection = new SqlConnection(BgExt.GetConnectionString(serviceProvider, configuration, "logistics")))
        {
            await connection.OpenAsync();
            SqlTransaction transaction = connection.BeginTransaction();
            try
            {
                var idShipment = Guid.NewGuid().ToString();
                vm.Shipment["Id"] = idShipment;

                var shipment = vm.Shipment.MapToDirPatch("Shipment");
                await _userSvc.SaveEntityTransaction(shipment, connection, transaction);

                foreach (var item in vm.EntityContainer)
                {
                    var quantity = Convert.ToInt32(item.GetValueOrDefault("Quantity", 1));
                    for (int index = 0; index < quantity; index++)
                    {
                        var id = Uuid7.Guid().ToString();
                        vm.ShipmentDetail["Id"] = id;
                        vm.ShipmentDetail["ParentId"] = idShipment;
                        vm.ShipmentDetail["ContainerTypeId"] = item["ContainerTypeId"];
                        vm.ShipmentDetail["ContainerNo"] = item["ContainerNo"];

                        var shipmentDetail = vm.ShipmentDetail.MapToDirPatch("Shipment");
                        await _userSvc.SaveEntityTransaction(shipmentDetail, connection, transaction);

                        bool isFirstContainer = index == 0;

                        await SaveFees(vm.ShipmentFee, id, connection, transaction, isFirstContainer);
                        await SaveFees(vm.ShipmentFee2, id, connection, transaction, isFirstContainer);
                        await SaveFees(vm.ShipmentFee3, id, connection, transaction, isFirstContainer);
                    }
                }
                await transaction.CommitAsync();
                return idShipment;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    [HttpPost("api/Forwardx/GenLogisticsShipment")]
    public async Task<string> GenLogisticsShipment([FromBody] TruckingShipmentVM vm)
    {
        using (SqlConnection connection = new SqlConnection(BgExt.GetConnectionString(serviceProvider, configuration, "logistics")))
        {
            await connection.OpenAsync();
            SqlTransaction transaction = connection.BeginTransaction();
            try
            {
                var idShipment = Guid.NewGuid().ToString();
                var idShipmentDetail = Guid.NewGuid().ToString();
                vm.Shipment["Id"] = idShipment;
                vm.ShipmentDetail["Id"] = idShipmentDetail;
                vm.ShipmentDetail["ParentId"] = idShipment;
                var shipment = vm.Shipment.MapToDirPatch("Shipment");
                var shipmentDetail = vm.ShipmentDetail.MapToDirPatch("Shipment");
                await _userSvc.SaveEntityTransaction(shipment, connection, transaction);
                await _userSvc.SaveEntityTransaction(shipmentDetail, connection, transaction);
                foreach (var item in vm.EntityContainer)
                {
                    var id = Uuid7.Guid().ToString();
                    item["Id"] = id;
                    item["EntityId"] = idShipment;
                    item["TableName"] = "Shipment";
                    var container = vm.ShipmentDetail.MapToDirPatch("EntityContainer");
                    await _userSvc.SaveEntityTransaction(container, connection, transaction);
                }
                await SaveFees(vm.ShipmentFee, idShipmentDetail, connection, transaction, true);
                await SaveFees(vm.ShipmentFee2, idShipmentDetail, connection, transaction, true);
                await SaveFees(vm.ShipmentFee3, idShipmentDetail, connection, transaction, true);
                await transaction.CommitAsync();
                return idShipment;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    private async Task SaveFees(List<Dictionary<string, object>> fees, string shipmentId, SqlConnection connection, SqlTransaction transaction, bool isFirstContainer)
    {
        foreach (var charge in fees)
        {
            bool isContainer = Convert.ToBoolean(charge.GetValueOrDefault("IsContainer", false));
            if (isFirstContainer || isContainer)
            {
                charge["Id"] = Uuid7.Guid().ToString();
                charge["ShipmentId"] = shipmentId;
                var chargePatch = charge.MapToDirPatch("ShipmentFee");
                await _userSvc.SaveEntityTransaction(chargePatch, connection, transaction);
            }
        }
    }
}