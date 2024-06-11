using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TourPlanner.BusinessLogic.Map
{
    public enum Marker
    {
        PIN_RED_16px,
        PIN_RED_32px,
        MARKER_RED_16px,
        MARKER_RED_32px
    }

    public static class MarkerUtils
    {
        private static readonly Dictionary<Marker, string> MarkerFilenames = new()
        {
            { Marker.PIN_RED_16px, "pin-red_16px" },
            { Marker.PIN_RED_32px, "pin-red_32px" },
            { Marker.MARKER_RED_16px, "marker-red_16px" },
            { Marker.MARKER_RED_32px, "marker-red_32px" }
        };

        public static string GetResource(Marker marker)
        {
            string filename = MarkerFilenames[marker] + ".png";
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources", filename);
        }

        public static Bitmap GetMarkerImage(Marker marker)
        {
            string resourcePath = GetResource(marker);
            return new Bitmap(resourcePath);
        }

    }
}
