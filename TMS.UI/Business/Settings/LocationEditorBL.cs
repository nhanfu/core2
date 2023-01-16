using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.Clients;
using System;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using static Retyped.googlemaps.google.maps;
using static Retyped.googlemaps.google.maps.places;

namespace TMS.UI.Business.Settings
{
    public class LocationEditorBL : PopupEditor
    {
        public const string GOOGLE_MAP_PLACES = "https://maps.googleapis.com/maps/api/js?key=AIzaSyBfVrTUFatsZTyqaCKwRzbj09DD72VxSwc&libraries=places";
        public Location LocationEntity => Entity as Location;
        public GridView gridView;
        public LocationEditorBL() : base(nameof(Location))
        {
            Name = "Location Editor";
            DOMContentLoaded += async () =>
            {
                await ApplyMap();
            };
        }

        public async Task ApplyMap()
        {
            await Client.LoadScript(GOOGLE_MAP_PLACES);
            InitMap();
        }

        public void InitMap()
        {
            var locationEntity = Entity as Location;
            var input = this.FindComponentByName<Textbox>(nameof(Location.DescriptionEn))
                .Element.As<Retyped.dom.HTMLInputElement>();
            var mapElement = this.FindComponentByName<Section>("Map").Element;
            var lat = LocationEntity.Lat == null ? 0 : LocationEntity.Lat;
            var lng = LocationEntity.Long == null ? 0 : LocationEntity.Long;
            var map = new Map(mapElement.As<Retyped.dom.Element>(), new MapOptions
            {
                center = new LatLng(lat, lng),
                zoom = 15,
                mapTypeId = MapTypeId.ROADMAP
            });
            var marker1 = new Marker(new MarkerOptions
            {
                position = new LatLng(lat, lng),
                map = map,
                draggable = true
            });

            marker1.addListener("dragend", (e) =>
            {
                var latitude = marker1.getPosition().lat();
                var longitude = marker1.getPosition().lng();
                LocationEntity.Lat = latitude;
                LocationEntity.Long = longitude;
                UpdateView(false, nameof(LocationEntity.Long), nameof(LocationEntity.Lat));
            });
            var searchBox = new SearchBox(input);
            map.controls[(int)ControlPosition.TOP_LEFT].push(input);
            map.addListener("bounds_changed", (e) =>
            {
                searchBox.setBounds(map.getBounds().As<LatLngBounds>());
            });

            var markers = new Marker[] { };
            searchBox.addListener("places_changed", (e) =>
            {
                var places = searchBox.getPlaces();
                if (places.Length == 0)
                {
                    return;
                }
                markers.ForEach((marker) =>
                {
                    marker.setMap(null as Map);
                });
                var bounds = new LatLngBounds();
                places.ForEach((place) =>
                {
                    LocationEntity.Lat = place.geometry.location.lat();
                    LocationEntity.Long = place.geometry.location.lng();
                    LocationEntity.DescriptionEn = input.value;
                    if (LocationEntity.Description1.IsNullOrWhiteSpace())
                    {
                        LocationEntity.Description1 = input.value;
                        UpdateView(false, nameof(LocationEntity.Description1));
                    }
                    else if (!LocationEntity.Description1.IsNullOrWhiteSpace() && LocationEntity.Description2.IsNullOrWhiteSpace())
                    {
                        LocationEntity.Description2 = input.value;
                        UpdateView(false, nameof(LocationEntity.Description2));
                    }
                    else if (!LocationEntity.Description1.IsNullOrWhiteSpace() && !LocationEntity.Description1.IsNullOrWhiteSpace() && LocationEntity.Description3.IsNullOrWhiteSpace())
                    {
                        LocationEntity.Description3 = input.value;
                        UpdateView(false, nameof(LocationEntity.Description3));
                    }
                    else if (place.geometry is null)
                    {
                        Console.WriteLine("Returned place contains no geometry");
                        return;
                    }
                    markers.Push(new Marker(new MarkerOptions
                    {
                        map = map,
                        title = place.name,
                        position = place.geometry.location
                    }));
                    markers.ForEach(x =>
                    {
                        x.addListener("dragend", (r) =>
                        {
                            var latitude = marker1.getPosition().lat();
                            var longitude = marker1.getPosition().lng();
                            LocationEntity.Lat = latitude;
                            LocationEntity.Long = longitude;
                            this.FindComponentByName<Number>(nameof(LocationEntity.Long)).UpdateView();
                            this.FindComponentByName<Number>(nameof(LocationEntity.Lat)).UpdateView();
                        });
                    });

                    if (place.geometry.viewport != null)
                    {
                        bounds.union(place.geometry.viewport);
                    }
                    else
                    {
                        bounds.extend(place.geometry.location);
                    }
                    Dirty = true;
                });
                map.fitBounds(bounds);
            });
        }

        public void CheckLocation(LocationService locationService, MasterData masterData)
        {
            gridView = gridView ?? this.FindActiveComponent<GridView>().FirstOrDefault();
            if (LocationEntity.LocationService.Any(x => x.Id != locationService.Id && x.ServiceId == masterData.Id))
            {
                Toast.Warning("Dữ liệu này đã tồn tại !!!");
                gridView.RemoveRow(locationService);
            }
        }
    }
}