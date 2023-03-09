using CommandLine;
using Roverlib.Models;
using Roverlib.Services;
using Roverlib.Utils;

internal class Program
{
    public class Options
    {
        [Option('t', "teams", Required = false, HelpText = "Number of teams to join")]
        public int NumTeams { get; set; }
        [Option('g', "game", Required = false, HelpText = "Game ID")]
        public string? GameId { get; set; }
        [Option('f', "flight", Required = false, HelpText = "Flight pattern (circle, target, spiral, clock))")]
        public string? FlightPattern { get; set; }
        [Option('u', "url", Required = false, HelpText = "URL of game server")]
        public string? Url { get; set; }
        [Option('e', "heuristic", Required = false, HelpText = "Heuristic to use (manhattan, euclidean)")]
        public string? Heuristic { get; set; }
        [Option('o', "optimization", Required = false, HelpText = "size of map buffer zone for pathfinding")]
        public int MapOptimizationBuffer { get; set; }
        [Option('q', "quickmode", Required = false, HelpText = "No helis, just go straight to target from nearest midpoint")]
        public bool QuickMode { get; set; }
    }
    private static async Task Main(string[] args)
    {
        var options = Parser.Default.ParseArguments<Options>(args).Value;

        HttpClient client = new HttpClient()
        {
            BaseAddress = new Uri(options.Url ?? "https://snow-rover.azurewebsites.net/")
            // BaseAddress = new Uri("https://localhost:64793/")
        };
        int NUM_TEAMS = options.NumTeams > 0 ? options.NumTeams : 10;
        string GAME_ID = options.GameId ?? "f";
        Func<(int, int), (int, int), int> heuristic = options.Heuristic switch
        {
            "manhattan" => PathFinder.ManhattanDistance,
            "euclidean" => PathFinder.EuclideanDistance,
            _ => PathFinder.ManhattanDistance
        };


        var trafficControl = new TrafficControlService(client);

        await trafficControl.JoinTeams(NUM_TEAMS, GAME_ID);

        await waitForPlayingStatusAsync(trafficControl);

        if (options.QuickMode)
        {
            var t = Task.Run(() => easyMoneyAsync(trafficControl));
        }
        else
        {
            trafficControl.FlyHeliFormation(formation: options.FlightPattern ?? "circle");
            var t = Task.Run(() => trafficControl.DriveRovers(heuristic, options.MapOptimizationBuffer > 5 ? options.MapOptimizationBuffer : 10));
        }

        while (true)
        { }

        static async Task easyMoneyAsync(TrafficControlService trafficControl)
        {
            var tasks = new List<Task>();
            foreach (var team in trafficControl.Teams)
            {
                //team.MoveRoverToPointAsync(trafficControl.GameBoard.Target.X, trafficControl.GameBoard.Target.Y);
                var task = team.MoveRoverToNearestAxisAsync(trafficControl);
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());
            foreach (var team in trafficControl.Teams)
            {
                var t = Task.Run(() => team.MoveRoverToPointAsync(trafficControl.GameBoard.Targets[0].X, trafficControl.GameBoard.Targets[0].Y));
            }
        }

        static async Task waitForPlayingStatusAsync(TrafficControlService trafficControl)
        {
            while (await trafficControl.CheckStatus() != TrafficControlService.GameStatus.Playing) ;
        }
    }
}