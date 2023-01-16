using Core.Extensions;
using System;
using TMS.API.Models;

namespace TMS.API.Extensions
{
    public static class MasterDataExt
    {
        public static decimal TryGetDecimal(this MasterData masterData)
        {
            if (masterData is null)
            {
                return 0;
            }
            return masterData.Description.TryParseDecimal() ?? 0;
        }

        public static double GetDistanceFromLatLonInKm(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371;
            var dLat = Deg2rad(lat2 - lat1);
            var dLon = Deg2rad(lon2 - lon1);
            var a =
              Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
              Math.Cos(Deg2rad(lat1)) * Math.Cos(Deg2rad(lat2)) *
              Math.Sin(dLon / 2) * Math.Sin(dLon / 2)
              ;
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = R * c; // Distance in km
            return d;
        }

        public static double Deg2rad(double deg)
        {
            return deg * (Math.PI / 180);
        }
    }
}
