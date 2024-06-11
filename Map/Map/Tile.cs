using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TourPlanner.BusinessLogic.Map
{
    internal class Tile
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Tile(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static Tile LatLonToTile(double latDeg, double lonDeg, int zoom)
        {
            double latRad = Math.PI / 180.0 * latDeg;
            double n = Math.Pow(2.0, zoom);
            int xTile = (int)Math.Floor((lonDeg + 180.0) / 360.0 * n);
            int yTile = (int)Math.Floor((1.0 - Math.Log(Math.Tan(latRad) + 1 / Math.Cos(latRad)) / Math.PI) / 2.0 * n);
            return new Tile(xTile, yTile);
        }
    }
}
