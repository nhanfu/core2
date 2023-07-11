using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using static Retyped.googlemaps;
using static Retyped.googlemaps.google.maps;

namespace TMS.UI.Business.Manage
{
    public class ViewMapTransportationBL : PopupEditor
    {
        public Transportation TransportationE => Entity as Transportation;
        public const string GOOGLE_MAP_PLACES = "https://maps.googleapis.com/maps/api/js?key=AIzaSyBfVrTUFatsZTyqaCKwRzbj09DD72VxSwc&libraries=places";
        public ViewMapTransportationBL() : base(nameof(Transportation))
        {
            Name = "ViewMap Transportation";
            DOMContentLoaded += async () =>
            {
                await ApplyMap();
            };
        }

        public async Task ApplyMap()
        {
            await Client.LoadScript(GOOGLE_MAP_PLACES);
            await InitMap();
        }

        public async Task InitMap()
        {
            var infoWindows = new List<InfoWindow>();
            var directionsRenderers = new List<DirectionsRenderer>();
            var routeReturnIds = (await new Client(nameof(UserRoute)).GetRawList<UserRoute>($"?$filter=TypeId eq 25044 and UserId eq {Client.Token.UserId}")).Select(x => x.RouteId).Where(x => x != null).Combine();
            var trans = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?&$top=200" +
                $"&$filter=(Id eq 40311  or Id eq {(TransportationE.EmptyCombinationId is null ? "null" : TransportationE.EmptyCombinationId.ToString())})  " +
                $"or (ReturnId ne null and ReturnDate ne null  " +
                $"and Cont40 eq {TransportationE.Cont40}  and Cont20 eq {TransportationE.Cont20} " +
                $"and CombinationId eq null and cast(ReturnDate,Edm.DateTimeOffset) lt {DateTimeExt.ToOdataFormat(TransportationE.ClosingDate.Value.AddDays(1))} " +
                $"and cast(ReturnDate,Edm.DateTimeOffset) gt {DateTimeExt.ToOdataFormat(TransportationE.ClosingDate.Value.AddDays(-3))}  " +
                $"and (ContainerNo eq '{TransportationE.ContainerNo}' or '{TransportationE.ContainerNo}' eq 'null' or '{TransportationE.ContainerNo}' eq '') {((routeReturnIds == "" || routeReturnIds == null) ? "" : $" and RouteId in ({routeReturnIds})")})&$orderby=ReturnDate desc");
            var ids = trans.Where(x => x.ReturnId != null).Select(x => x.ReturnId.Value).ToList();
            var containerTypeIds = trans.Where(x => x.ContainerTypeId != null).Select(x => x.ContainerTypeId.Value).ToList();
            var commodityIds = trans.Where(x => x.CommodityId != null).Select(x => x.CommodityId.Value).ToList();
            var shipIds = trans.Where(x => x.ShipId != null).Select(x => x.ShipId.Value).ToList();
            var brandShipIds = trans.Where(x => x.BrandShipId != null).Select(x => x.BrandShipId.Value).ToList();
            var returnVendorIds = trans.Where(x => x.ReturnVendorId != null).Select(x => x.ReturnVendorId.Value).ToList();
            var socIds = trans.Where(x => x.SocId != null).Select(x => x.SocId.Value).ToList();
            ids.Add(TransportationE.ReceivedId.Value);
            var receivedsDb = new Client(nameof(Location)).GetRawListById<Location>(ids);
            var containerTypeDb = new Client(nameof(MasterData)).GetRawListById<MasterData>(containerTypeIds);
            var commodityDb = new Client(nameof(MasterData)).GetRawListById<MasterData>(commodityIds);
            var shipDb = new Client(nameof(Ship)).GetRawListById<Ship>(shipIds);
            var brandShipDb = new Client(nameof(Vendor)).GetRawListById<Vendor>(brandShipIds);
            var returnVendorDb = new Client(nameof(Vendor)).GetRawListById<Vendor>(returnVendorIds);
            var socDb = new Client(nameof(Vendor)).GetRawListById<Vendor>(socIds);
            await Task.WhenAll(receivedsDb, containerTypeDb, commodityDb, shipDb, brandShipDb, returnVendorDb, socDb);
            var receiveds = receivedsDb.Result;
            var containerTypes = containerTypeDb.Result;
            var commoditys = commodityDb.Result;
            var ships = shipDb.Result;
            var brandShips = brandShipDb.Result;
            var returnVendors = returnVendorDb.Result;
            var socs = socDb.Result;

            var mapElement = this.FindComponentByName<Section>("Map").Element;
            var received = receiveds.FirstOrDefault(x => x.Id == TransportationE.ReceivedId);
            var lat = received.Lat;
            var lng = received.Long;
            var map = new Map(mapElement.As<Retyped.dom.Element>(), new MapOptions
            {
                center = new LatLng(lat, lng),
                zoom = 10,
                mapTypeId = MapTypeId.ROADMAP
            });
            var infoWindowE = new InfoWindow();
            infoWindowE.setContent($@"<ul style='margin: 0;padding: 0;'><li  style='text-align: left;'>Địa điểm: {received.Description}</li>
                    <li  style='text-align: left;'>Số cont: {TransportationE.ContainerNo}</li>
                    <li style='text-align: left;'>Loại cont: {(TransportationE["ContainerType"] is null ? "" : TransportationE["ContainerType"]["Description"])}</li>
                    <li style='text-align: left;'>Mặt hàng: {(TransportationE["Commodity"] is null ? "" : TransportationE["Commodity"]["Description"])}</li>
                    <li  style='text-align: left;'>SOC: {(TransportationE["Soc"] is null ? "" : TransportationE["Soc"]["Name"])}</li>
                    <li  style='text-align: left;'>Hãng tàu: {(TransportationE["BrandShip"] is null ? "" : TransportationE["BrandShip"]["Name"])}</li>
                    <li  style='text-align: left;'>Tên tàu: {(TransportationE["Ship"] is null ? "" : TransportationE["Ship"]["Name"])}</li>
                    <li  style='text-align: left;'>Số chuyến: {TransportationE.Trip}</li>
                    <li  style='text-align: left;'>Chủ hàng : {TransportationE.Note4}</li>
                    <li  style='text-align: left;'>Ngày đóng hàng : {TransportationE.ClosingDate:dd/MM/yyyy}</li></ul>");
            var marker1 = new Marker(new MarkerOptions
            {
                position = new LatLng(lat, lng),
                map = map,
                icon = "/icons/Fcl.png",
            });
            infoWindows.Add(infoWindowE);
            marker1.addListener("click", (e) =>
            {
                foreach (var inf in infoWindows)
                {
                    inf.close();
                }
                infoWindowE.open(map, marker1);
            });
            trans.Where(x => x.Id != TransportationE.ReturnId).ForEach(tran =>
            {
                var re = receiveds.FirstOrDefault(x => x.Id == tran.ReturnId);
                var cont = containerTypes.FirstOrDefault(x => x.Id == tran.ContainerTypeId);
                var ship = ships.FirstOrDefault(x => x.Id == tran.ShipId);
                var brs = brandShips.FirstOrDefault(x => x.Id == tran.BrandShipId);
                var com = commoditys.FirstOrDefault(x => x.Id == tran.CommodityId);
                var rev = returnVendors.FirstOrDefault(x => x.Id == tran.ReturnVendorId);
                var soc = socs.FirstOrDefault(x => x.Id == tran.SocId);
                if (re is null)
                {
                    return;
                }
                var marker = new Marker(new MarkerOptions
                {
                    position = new LatLng(re.Lat, re.Long),
                    map = map,
                    icon = "/icons/TransportRequest.png"
                });
                marker.addListener("click", (e) =>
                {
                    foreach (var inf in infoWindows)
                    {
                        inf.close();
                    }
                    foreach (var inf in directionsRenderers)
                    {
                        inf.setMap(null);
                    }
                    var directionsRenderer = new DirectionsRenderer();
                    directionsRenderer.setMap(map);
                    var directionsService = new DirectionsService();
                    //var route = directionsRenderer.getDirections().routes[0];
                    //var duration = route.legs[0].duration.text;
                    var infoWindow = new InfoWindow();
                    infoWindow.setContent($@"<ul style='margin: 0;padding: 0;'><li  style='text-align: left;'>Địa điểm: {re.Description.DecodeSpecialChar()}</li>
                    <li style='text-align: left;'>Số cont: {tran.ContainerNo}</li>
                    <li style='text-align: left;'>Loại cont: {(cont is null ? "" : cont.Description.DecodeSpecialChar())}</li>
                    <li style='text-align: left;'>Mặt hàng: {(com is null ? "" : com.Description.DecodeSpecialChar())}</li>
                    <li style='text-align: left;'>SOC: {(soc is null ? "" : soc.Name.DecodeSpecialChar())}</li>
                    <li style='text-align: left;'>Hãng tàu: {(brs is null ? "" : brs.Name.DecodeSpecialChar())}</li>
                    <li style='text-align: left;'>Tên tàu: {(ship is null ? "" : ship.Name.DecodeSpecialChar())}</li>
                    <li style='text-align: left;'>Số chuyến: {tran.Trip}</li>
                    <li style='text-align: left;'>Chủ hàng : {tran.Note4}</li>
                    <li style='text-align: left;'>Đơn vị trả hàng : {(rev is null ? "" : rev.Name.DecodeSpecialChar())}</li>
                    <li style='text-align: left;'>Ngày trả hàng : {tran.ReturnDate:dd/MM/yyyy}</li>
                    <li style='text-align: left;'>Thời gian : </li></ul>");
                    infoWindows.Add(infoWindow);
                    /*@
                     directionsService
                    .route({
                      origin: { lat: lat, lng: lng },
                      destination: { lat: re.Lat, lng: re.Long },
                      travelMode: 'DRIVING',
                    })
                    .then((response) => {
                      directionsRenderer.setDirections(response);
                    })
                    .catch((e) => {});
                     */
                    directionsRenderers.Add(directionsRenderer);
                    infoWindow.open(map, infoWindow);
                });
            });
        }
    }
}