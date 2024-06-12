using SkiaSharp;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace TourPlanner.BusinessLogic.Map
{
    internal class MapAPIService
    {
        private readonly string API_KEY;

        public MapAPIService(string apiKey)
        {
            this.API_KEY = apiKey;
        }

        public async Task<GeoCoordinate> GetGeoCodeAsync(string address)
        {
            string uri = $"https://api.openrouteservice.org/geocode/search?api_key={API_KEY}&text={address}";
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(uri);
                return ParseGeocodeResponse(await response.Content.ReadAsStringAsync());
            }
        }

        public async Task<SKBitmap> GetTileAsync(Tile tile, int zoom)
        {
            string uri = $"https://tile.openstreetmap.org/{zoom}/{tile.X}/{tile.Y}.png";
            using (HttpClient client = new HttpClient())
            {
                // Adding a custom User-Agent header
                client.DefaultRequestHeaders.UserAgent.ParseAdd("TourPlanner/1.0 Project for the SWEN-course");

                HttpResponseMessage response = await client.GetAsync(uri);

                // Check if the HTTP request was successful
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Failed to fetch tile image. Status code: {response.StatusCode}");
                }

                using (Stream stream = await response.Content.ReadAsStreamAsync())
                {
                    // Check if the stream is valid
                    if (stream == null || stream.Length == 0)
                    {
                        throw new ArgumentException("Stream is null or empty.");
                    }

                    return SKBitmap.Decode(stream);
                }
            }
        }

        public async Task<string> GetDirectionsAsync(string startCoordinates, string endCoordinates)
        {
            string uri = $"https://api.openrouteservice.org/v2/directions/driving-car?api_key={API_KEY}&start={startCoordinates}&end={endCoordinates}";
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(uri);
                return await response.Content.ReadAsStringAsync();
            }
        }

        public GeoCoordinate ParseGeocodeResponse(string response)
        {
            JsonDocument doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            // Parse coordinates
            var coordinates = root.GetProperty("features")[0].GetProperty("geometry").GetProperty("coordinates");
            GeoCoordinate coordinatesData = new GeoCoordinate(coordinates[0].GetDouble(), coordinates[1].GetDouble());

            return coordinatesData;
        }
    }
}
