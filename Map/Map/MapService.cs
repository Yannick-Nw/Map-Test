using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Windows.Devices.Geolocation;
using System.Configuration;

namespace TourPlanner.BusinessLogic.Map
{
    internal class MapService
    {
        private Bitmap finalImage;

        public async Task<string> GetMap(string adressStart, string adressEnd)
        {
            string apiKey = "5b3ce3597851110001cf6248cc5cdbbf7c0b4192a6dac8519aa7c442";
            var request = new MapAPIService(apiKey);
            GeoCoordinate locationDataStart = await request.GetGeoCodeAsync(adressStart);
            GeoCoordinate locationDataEnd = await request.GetGeoCodeAsync(adressEnd);
            MapCreator mapCreator = new MapCreator(locationDataStart, locationDataEnd);
            mapCreator.Zoom = 18;
            mapCreator.AddMarker(locationDataStart);
            mapCreator.AddMarker(locationDataEnd);

            finalImage = await mapCreator.GenerateImage(request);
            string filePath = SaveImage(adressStart + "-" + adressEnd);
            return filePath;
        }

        public string SaveImage(string filename)
        {
            string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "Images");
            Directory.CreateDirectory(directoryPath); // Creates the directory if it doesn't exist

            string filePath = Path.Combine(directoryPath, filename + ".png");
            finalImage.Save(filePath, ImageFormat.Png);

            return filePath;
        }

    }
}
