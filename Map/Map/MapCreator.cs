using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace TourPlanner.BusinessLogic.Map
{
    internal class MapCreator
    {
        private readonly double minLon;
        private readonly double minLat;
        private readonly double maxLon;
        private readonly double maxLat;

        public MapCreator(GeoCoordinate start, GeoCoordinate end)
        {
            this.minLon = Math.Min(start.Lon, end.Lon);
            this.minLat = Math.Min(start.Lat, end.Lat);
            this.maxLon = Math.Max(start.Lon, end.Lon);
            this.maxLat = Math.Max(start.Lat, end.Lat);
        }

        public int Zoom { get; set; } = 18;
        public bool CropImage { get; set; } = true;

        private readonly List<GeoCoordinate> markers = new();
        private readonly List<GeoCoordinate> routeWaypoints = new();
        private Bitmap finalImage;

        public void AddMarker(GeoCoordinate marker)
        {
            markers.Add(marker);
        }

        public void AddRouteWaypoints(List<GeoCoordinate> waypoints)
        {
            routeWaypoints.AddRange(waypoints);
        }

        public async Task<Bitmap> GenerateImage(MapAPIService api)
        {
            const int maxTileSize = 256;
            const int maxBitmapDimension = 10000; // Adjust this limit based on your system capabilities

            // Calculate the tile numbers for each corner of the bounding box
            var topLeftTile = Tile.LatLonToTile(maxLat, minLon, Zoom);
            var bottomRightTile = Tile.LatLonToTile(minLat, maxLon, Zoom);

            // Debugging output for tile positions
            Console.WriteLine($"Top Left Tile: X={topLeftTile.X}, Y={topLeftTile.Y}");
            Console.WriteLine($"Bottom Right Tile: X={bottomRightTile.X}, Y={bottomRightTile.Y}");

            // Determine the number of tiles to fetch in each dimension
            int tilesX = bottomRightTile.X - topLeftTile.X + 1;
            int tilesY = bottomRightTile.Y - topLeftTile.Y + 1;

            // Debugging output for calculated dimensions
            Console.WriteLine($"tilesX: {tilesX}, tilesY: {tilesY}");

            int totalWidth = tilesX * maxTileSize;
            int totalHeight = tilesY * maxTileSize;

            Console.WriteLine($"Total Width: {totalWidth}, Total Height: {totalHeight}");

            // Create the final image in smaller parts
            Bitmap finalImage = new Bitmap(totalWidth, totalHeight);
            using (Graphics finalGraphics = Graphics.FromImage(finalImage))
            {
                for (int x = 0; x < tilesX; x++)
                {
                    for (int y = 0; y < tilesY; y++)
                    {
                        int tileWidth = Math.Min(maxTileSize, (tilesX - x) * maxTileSize);
                        int tileHeight = Math.Min(maxTileSize, (tilesY - y) * maxTileSize);

                        Bitmap tileImage = new Bitmap(tileWidth, tileHeight);
                        using (Graphics tileGraphics = Graphics.FromImage(tileImage))
                        {
                            int globalX = topLeftTile.X + x;
                            int globalY = topLeftTile.Y + y;

                            if (globalX <= bottomRightTile.X && globalY <= bottomRightTile.Y)
                            {
                                Bitmap fetchedTile;
                                try
                                {
                                    fetchedTile = await api.GetTileAsync(new Tile(globalX, globalY), Zoom);
                                }
                                catch (Exception e)
                                {
                                    throw new InvalidOperationException(
                                        $"Failed to fetch tile image for X={globalX}, Y={globalY}.", e);
                                }

                                if (fetchedTile == null)
                                {
                                    throw new InvalidOperationException(
                                        $"Tile image for X={globalX}, Y={globalY} is null.");
                                }

                                int xPos = 0;
                                int yPos = 0;
                                tileGraphics.DrawImage(fetchedTile, xPos, yPos);
                            }
                        }

                        int finalXPos = x * maxTileSize;
                        int finalYPos = y * maxTileSize;
                        finalGraphics.DrawImage(tileImage, finalXPos, finalYPos);
                    }
                }

                Point topLeftTilePixel = new Point(topLeftTile.X * maxTileSize, topLeftTile.Y * maxTileSize);

                // Draw route waypoints as lines
                if (routeWaypoints.Count > 1)
                {
                    using (Pen pen = new Pen(Color.Red, 10))
                    {
                        for (int i = 0; i < routeWaypoints.Count - 1; i++)
                        {
                            Point point1 = Point.LatLonToPixel(routeWaypoints[i].Lat, routeWaypoints[i].Lon, Zoom);
                            Point point2 = Point.LatLonToPixel(routeWaypoints[i + 1].Lat, routeWaypoints[i + 1].Lon,
                                Zoom);

                            Point relativePos1 = new Point(point1.X - topLeftTilePixel.X,
                                point1.Y - topLeftTilePixel.Y);
                            Point relativePos2 = new Point(point2.X - topLeftTilePixel.X,
                                point2.Y - topLeftTilePixel.Y);

                            // Ensure points are within image bounds before drawing
                            if (IsWithinBounds(relativePos1, finalImage.Width, finalImage.Height) &&
                                IsWithinBounds(relativePos2, finalImage.Width, finalImage.Height))
                            {
                                finalGraphics.DrawLine(pen, relativePos1.X, relativePos1.Y, relativePos2.X,
                                    relativePos2.Y);
                            }
                        }
                    }
                }

                // Draw Markers
                foreach (var marker in markers)
                {
                    Bitmap markerIcon = MarkerUtils.GetMarkerImage(Marker.PIN_RED_32px);
                    Point globalPos = Point.LatLonToPixel(marker.Lat, marker.Lon, Zoom);
                    Point relativePos = new Point(globalPos.X - topLeftTilePixel.X, globalPos.Y - topLeftTilePixel.Y);

                    // Ensure marker is within image bounds before drawing
                    if (IsWithinBounds(relativePos, finalImage.Width, finalImage.Height))
                    {
                        finalGraphics.DrawImage(markerIcon, relativePos.X, relativePos.Y);
                    }
                }

                // Crop the image to the exact bounding box
                if (CropImage)
                {
                    Point bboxLeftTopGlobalPos = Point.LatLonToPixel(maxLat, minLon, Zoom);
                    Point bboxRightBottomGlobalPos = Point.LatLonToPixel(minLat, maxLon, Zoom);
                    Point bboxLeftTopRelativePos = new Point(bboxLeftTopGlobalPos.X - topLeftTilePixel.X,
                        bboxLeftTopGlobalPos.Y - topLeftTilePixel.Y);
                    int width = bboxRightBottomGlobalPos.X - bboxLeftTopGlobalPos.X;
                    int height = bboxRightBottomGlobalPos.Y - bboxLeftTopGlobalPos.Y;

                    Console.WriteLine(
                        $"bboxLeftTopRelativePos: X={bboxLeftTopRelativePos.X}, Y={bboxLeftTopRelativePos.Y}");
                    Console.WriteLine($"Width: {width}, Height: {height}");

                    // Ensure width and height are valid
                    if (width > 0 && height > 0)
                    {
                        try
                        {
                            finalImage =
                                finalImage.Clone(
                                    new Rectangle(bboxLeftTopRelativePos.X, bboxLeftTopRelativePos.Y, width, height),
                                    finalImage.PixelFormat);
                        }
                        catch (Exception e)
                        {
                            throw new ArgumentException("Cropping the image failed due to invalid dimensions.", e);
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Invalid dimensions for the cropped image.");
                    }
                }
            }

            return finalImage;
        }

        public async Task AddRouteAsync(MapAPIService api, GeoCoordinate start, GeoCoordinate end)
        {
            // Ensure the correct decimal separator
            string startCoordinates =
                $"{start.Lon.ToString(System.Globalization.CultureInfo.InvariantCulture)},{start.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            string endCoordinates =
                $"{end.Lon.ToString(System.Globalization.CultureInfo.InvariantCulture)},{end.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

            string directionsJson = await api.GetDirectionsAsync(startCoordinates, endCoordinates);

            // Log the JSON response
            //Console.WriteLine(directionsJson);

            var waypoints = ParseWaypoints(directionsJson);

            AddRouteWaypoints(waypoints);
        }

        private List<GeoCoordinate> ParseWaypoints(string directionsJson)
        {
            var waypoints = new List<GeoCoordinate>();
            using (JsonDocument doc = JsonDocument.Parse(directionsJson))
            {
                var root = doc.RootElement;

                if (root.TryGetProperty("features", out JsonElement features))
                {
                    if (features.GetArrayLength() > 0 &&
                        features[0].TryGetProperty("geometry", out JsonElement geometry))
                    {
                        if (geometry.TryGetProperty("coordinates", out JsonElement coordinates))
                        {
                            foreach (var coordinate in coordinates.EnumerateArray())
                            {
                                var lon = coordinate[0].GetDouble();
                                var lat = coordinate[1].GetDouble();
                                waypoints.Add(new GeoCoordinate(lon, lat));
                            }
                        }
                        else
                        {
                            Console.WriteLine("No 'coordinates' property found in 'geometry'.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No 'geometry' property found in the first feature.");
                    }
                }
                else
                {
                    Console.WriteLine("No 'features' property found in the root element.");
                }
            }

            return waypoints;
        }

        private bool IsWithinBounds(Point point, int width, int height)
        {
            return point.X >= 0 && point.X < width && point.Y >= 0 && point.Y < height;
        }
    }
}