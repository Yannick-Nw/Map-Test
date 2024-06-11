using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TourPlanner.BusinessLogic.Map
{
    internal class GeoCoordinate
    {
        public double Lon { get; set; }
        public double Lat { get; set; }

        public GeoCoordinate(double lon, double lat)
        {
            Lon = lon;
            Lat = lat;
        }
    }
}
