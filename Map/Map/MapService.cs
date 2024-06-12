using SkiaSharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TourPlanner.BusinessLogic.Map
{
    internal class MapService
    {
        private SKBitmap finalImage;

        public async Task<string> GetMap(string addressStart, string addressEnd)
        {
            string apiKey = "5b3ce3597851110001cf6248cc5cdbbf7c0b4192a6dac8519aa7c442";
            var request = new MapAPIService(apiKey);

            // Get geo-coordinates
            GeoCoordinate locationDataStart = await request.GetGeoCodeAsync(addressStart);
            GeoCoordinate locationDataEnd = await request.GetGeoCodeAsync(addressEnd);

            // Create the map
            MapCreator mapCreator = new MapCreator(locationDataStart, locationDataEnd)
            {
                Zoom = 18
            };

            // Add markers for the start and end points
            mapCreator.AddMarker(locationDataStart);
            mapCreator.AddMarker(locationDataEnd);

            // Add the route with waypoints
            await mapCreator.AddRouteAsync(request, locationDataStart, locationDataEnd);

            // Generate the image
            finalImage = await mapCreator.GenerateImage(request);
            string filePath = SaveImage(addressStart + "-" + addressEnd);
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

                // Save the image using SkiaSharp
                using (var image = SKImage.FromBitmap(finalImage))
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                using (var stream = File.OpenWrite(filePath))
                {
                    data.SaveTo(stream);
                }

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
