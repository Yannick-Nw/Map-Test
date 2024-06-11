// See https://aka.ms/new-console-template for more information

using TourPlanner.BusinessLogic.Map;

Console.WriteLine("Hello, World!");
MapService mapService = new MapService();
string filePath = await mapService.GetMap("Höchstädtpl. 6, 1200 Wien", "Universitätsring 1, 1010 Wien");
Console.WriteLine(filePath);
