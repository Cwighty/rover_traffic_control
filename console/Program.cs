using CommandLine;
using Roverlib.Models;
using Roverlib.Services;

internal class Program
{
    public class Options
    {
        [Option('t', "teams", Required = false, HelpText = "Number of teams to join")]
        public int NumTeams { get; set; } = 10;
        [Option('g', "game", Required = false, HelpText = "Game ID")]
        public string GameId { get; set; } = "a";
        [Option('f', "flight", Required = false, HelpText = "Flight pattern (circle, target, spiral, clock))")]
        public string? FlightPattern { get; set; } = "circle";
        [Option('u', "url", Required = false, HelpText = "URL of game server")]
        //public string Url { get; set; } = "https://snow-rover.azurewebsites.net/";
        public string Url { get; set; } = "https://localhost:7287/";
        [Option('e', "heuristic", Required = false, HelpText = "Heuristic to use (manhattan, euclidean)")]
        public string Heuristic { get; set; } = "manhattan";
        [Option('o', "optimization", Required = false, HelpText = "size of map buffer zone for pathfinding")]
        public int MapOptimizationBuffer { get; set; } = 20;
        [Option('q', "quickmode", Required = false, HelpText = "No helis, for when battery doesnt matter.")]
        public bool QuickMode { get; set; } = false;
    }
    private static async Task Main(string[] args)
    {
        var options = Parser.Default.ParseArguments<Options>(args).Value;

        HttpClient client = new HttpClient()
        {
            BaseAddress = new Uri(options.Url)
        };

        Func<(int, int), (int, int), int> heuristic = options.Heuristic switch
        {
            "manhattan" => PathFinder.ManhattanDistance,
            "euclidean" => PathFinder.EuclideanDistance,
            _ => PathFinder.ManhattanDistance
        };

        var trafficControl = new TrafficControlService(client);
        trafficControl.GameWonEvent += async (sender, e) =>
        {
            await ReconAndCacheMap(trafficControl);
        };


        await trafficControl.JoinTeams(options.NumTeams, options.GameId);
        var filePath = $"../maps/{MapHelper.GetFileNameFromMap(trafficControl.GameBoard.LowResMap)}";
        await waitForPlayingStatusAsync(trafficControl);

        if (options.QuickMode)
        {
            trafficControl.GameBoard.VisitedNeighbors = MapHelper.InitializeDefaultMap(trafficControl.GameBoard.LowResMap);
            trafficControl.DriveRoversToTargets(heuristic, options.MapOptimizationBuffer);
        }
        else
        {
            // get the file path
            // check if the file exists
            if (File.Exists(filePath))
            {
                // read the map from the file
                trafficControl.GameBoard.VisitedNeighbors = MapHelper.ReadMapFromCSV(filePath);
            }
            else
            {
                // create the map from the low resolution map
                trafficControl.FlyHelisToTargets();
            }
            trafficControl.DriveRoversToTargets(heuristic, options.MapOptimizationBuffer);
        }
        while (true)
        {
            await Task.Delay(1000);
            MapHelper.WriteMapToCSV(trafficControl.GameBoard.VisitedNeighbors, filePath, trafficControl.GameBoard.LowResMap);
        }
    }

    private static async Task ReconAndCacheMap(TrafficControlService trafficControl)
    {
        trafficControl.CancelAll();
        await trafficControl.FlyHeliReconMission();
    }

    private static async Task waitForPlayingStatusAsync(TrafficControlService trafficControl)
    {
        while (await trafficControl.CheckStatus() != GameStatus.Playing) ;
    }
}