// See https://aka.ms/new-console-template for more information

using TourPlanner.BusinessLogic.Map;

Console.WriteLine("Map-Test Start...");
MapService mapService = new MapService();
string filePath = await mapService.GetMap("Wien", "Horn");
Console.WriteLine(filePath);
