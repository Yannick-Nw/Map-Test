using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

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
            try
            {
                string directoryPath = FindDirectoryWithImages(AppContext.BaseDirectory);

                if (directoryPath == null)
                {
                    throw new DirectoryNotFoundException("Images directory not found.");
                }

                string filePath = Path.Combine(directoryPath, filename + ".png");
                Console.WriteLine($"Saving image to: {filePath}");

                finalImage.Save(filePath, ImageFormat.Png);

                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        private static string FindDirectoryWithImages(string startDirectory)
        {
            string currentDirectory = startDirectory;
            while (!string.IsNullOrEmpty(currentDirectory))
            {
                string potentialPath = Path.Combine(currentDirectory, "Map", "Images");
                if (Directory.Exists(potentialPath))
                {
                    return potentialPath;
                }
                currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
            }

            // Create the directory if it doesn't exist
            string targetPath = Path.Combine(AppContext.BaseDirectory, "Map", "Images");
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }
            return targetPath;
        }
    }
}
