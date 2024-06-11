using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TourPlanner.BusinessLogic.Map
{
    internal class Point
    {
        public int X { get; }
        public int Y { get; }

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static Point LatLonToPixel(double lat, double lon, int zoom)
        {
            double latRad = lat * (Math.PI / 180.0);
            double n = Math.Pow(2.0, zoom);
            int xPixel = (int)Math.Floor((lon + 180.0) / 360.0 * n * 256);
            int yPixel = (int)Math.Floor((1.0 - Math.Log(Math.Tan(latRad) + 1 / Math.Cos(latRad)) / Math.PI) / 2.0 * n * 256);

            return new Point(xPixel, yPixel);
        }
    }
}
