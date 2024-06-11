using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Drawing;
using System.IO;
using System.Globalization;
using System.Text.Json;

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

        public async Task<Bitmap> GetTileAsync(Tile tile, int zoom)
        {
            string uri = $"https://tile.openstreetmap.org/{zoom}/{tile.X}/{tile.Y}.png";
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(uri);

                // Überprüfen, ob der HTTP-Request erfolgreich war
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Failed to fetch tile image. Status code: {response.StatusCode}");
                }

                using (Stream stream = await response.Content.ReadAsStreamAsync())
                {
                    // Überprüfen, ob der Stream gültig ist
                    if (stream == null || stream.Length == 0)
                    {
                        throw new ArgumentException("Stream is null or empty.");
                    }

                    try
                    {
                        return new Bitmap(stream);
                    }
                    catch (ArgumentException ex)
                    {
                        throw new ArgumentException("The stream does not contain a valid image.", ex);
                    }
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

            return (coordinatesData);
        }
    }
}
