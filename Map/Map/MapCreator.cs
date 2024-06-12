using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
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

        public void AddMarker(GeoCoordinate marker)
        {
            markers.Add(marker);
        }

        public void AddRouteWaypoints(List<GeoCoordinate> waypoints)
        {
            routeWaypoints.AddRange(waypoints);
        }

        public async Task<SKBitmap> GenerateImage(MapAPIService api)
        {
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

            int totalWidth = tilesX * 256;
            int totalHeight = tilesY * 256;

            Console.WriteLine($"Total Width: {totalWidth}, Total Height: {totalHeight}");

            // Create the final image in smaller parts
            var finalImage = new SKBitmap(totalWidth, totalHeight);
            using (var canvas = new SKCanvas(finalImage))
            {
                for (int x = 0; x < tilesX; x++)
                {
                    for (int y = 0; y < tilesY; y++)
                    {
                        int globalX = topLeftTile.X + x;
                        int globalY = topLeftTile.Y + y;

                        if (globalX <= bottomRightTile.X && globalY <= bottomRightTile.Y)
                        {
                            SKBitmap tileImage;
                            try
                            {
                                tileImage = await api.GetTileAsync(new Tile(globalX, globalY), Zoom);
                            }
                            catch (Exception e)
                            {
                                throw new InvalidOperationException(
                                    $"Failed to fetch tile image for X={globalX}, Y={globalY}.", e);
                            }

                            if (tileImage == null)
                            {
                                throw new InvalidOperationException(
                                    $"Tile image for X={globalX}, Y={globalY} is null.");
                            }

                            int xPos = x * 256;
                            int yPos = y * 256;
                            canvas.DrawBitmap(tileImage, xPos, yPos);
                        }
                    }
                }

                SKPoint topLeftTilePixel = new SKPoint(topLeftTile.X * 256, topLeftTile.Y * 256);

                // Draw route waypoints as lines
                if (routeWaypoints.Count > 1)
                {
                    var paint = new SKPaint
                    {
                        Color = SKColors.Red,
                        StrokeWidth = 10,
                        IsStroke = true
                    };
                    for (int i = 0; i < routeWaypoints.Count - 1; i++)
                    {
                        SKPoint point1 = LatLonToPixel(routeWaypoints[i].Lat, routeWaypoints[i].Lon, Zoom);
                        SKPoint point2 = LatLonToPixel(routeWaypoints[i + 1].Lat, routeWaypoints[i + 1].Lon, Zoom);

                        SKPoint relativePos1 =
                            new SKPoint(point1.X - topLeftTilePixel.X, point1.Y - topLeftTilePixel.Y);
                        SKPoint relativePos2 =
                            new SKPoint(point2.X - topLeftTilePixel.X, point2.Y - topLeftTilePixel.Y);

                        // Ensure points are within image bounds before drawing
                        if (IsWithinBounds(relativePos1, finalImage.Width, finalImage.Height) &&
                            IsWithinBounds(relativePos2, finalImage.Width, finalImage.Height))
                        {
                            canvas.DrawLine(relativePos1, relativePos2, paint);
                        }
                    }
                }

                // Draw Markers
                foreach (var marker in markers)
                {
                    SKBitmap markerIcon = MarkerUtils.GetMarkerImage(Marker.PIN_RED_32px);
                    SKPoint globalPos = LatLonToPixel(marker.Lat, marker.Lon, Zoom);
                    SKPoint relativePos =
                        new SKPoint(globalPos.X - topLeftTilePixel.X, globalPos.Y - topLeftTilePixel.Y);

                    // Ensure marker is within image bounds before drawing
                    if (IsWithinBounds(relativePos, finalImage.Width, finalImage.Height))
                    {
                        canvas.DrawBitmap(markerIcon, relativePos);
                    }
                }

                // Crop the image to the exact bounding box
                if (CropImage)
                {
                    SKPoint bboxLeftTopGlobalPos = LatLonToPixel(maxLat, minLon, Zoom);
                    SKPoint bboxRightBottomGlobalPos = LatLonToPixel(minLat, maxLon, Zoom);
                    SKPoint bboxLeftTopRelativePos = new SKPoint(bboxLeftTopGlobalPos.X - topLeftTilePixel.X,
                        bboxLeftTopGlobalPos.Y - topLeftTilePixel.Y);
                    int width = (int)(bboxRightBottomGlobalPos.X - bboxLeftTopGlobalPos.X);
                    int height = (int)(bboxRightBottomGlobalPos.Y - bboxLeftTopGlobalPos.Y);

                    Console.WriteLine(
                        $"bboxLeftTopRelativePos: X={bboxLeftTopRelativePos.X}, Y={bboxLeftTopRelativePos.Y}");
                    Console.WriteLine($"Width: {width}, Height: {height}");

                    // Ensure width and height are valid
                    if (width > 0 && height > 0)
                    {
                        finalImage = new SKBitmap(width, height);
                        using (var croppedCanvas = new SKCanvas(finalImage))
                        {
                            croppedCanvas.DrawBitmap(finalImage,
                                new SKRect(bboxLeftTopRelativePos.X, bboxLeftTopRelativePos.Y,
                                    bboxLeftTopRelativePos.X + width, bboxLeftTopRelativePos.Y + height),
                                new SKRect(0, 0, width, height));
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

        private bool IsWithinBounds(SKPoint point, int width, int height)
        {
            return point.X >= 0 && point.X < width && point.Y >= 0 && point.Y < height;
        }

        public static SKPoint LatLonToPixel(double lat, double lon, int zoom)
        {
            // Conversion logic here
            // This is just a placeholder implementation
            return new SKPoint((float)(lon * zoom), (float)(lat * zoom));
        }

        public async Task AddRouteAsync(MapAPIService api, GeoCoordinate start, GeoCoordinate end)
        {
            string startCoordinates =
                $"{start.Lon.ToString(System.Globalization.CultureInfo.InvariantCulture)},{start.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            string endCoordinates =
                $"{end.Lon.ToString(System.Globalization.CultureInfo.InvariantCulture)},{end.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

            string directionsJson = await api.GetDirectionsAsync(startCoordinates, endCoordinates);

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
    }
}