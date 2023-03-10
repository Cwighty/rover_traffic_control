using CommandLine;
using Roverlib.Services;

internal class Program
{
    public class Options
    {
        [Option('t', "teams", Required = false, HelpText = "Number of teams to join")]
        public int NumTeams { get; set; } = 10;
        [Option('g', "game", Required = false, HelpText = "Game ID")]
        public string GameId { get; set; } = "j";
        [Option('f', "flight", Required = false, HelpText = "Flight pattern (circle, target, spiral, clock))")]
        public string? FlightPattern { get; set; } = "circle";
        [Option('u', "url", Required = false, HelpText = "URL of game server")]
        public string Url { get; set; } = "https://snow-rover.azurewebsites.net/";
        [Option('e', "heuristic", Required = false, HelpText = "Heuristic to use (manhattan, euclidean)")]
        public string Heuristic { get; set; } = "manhattan";
        [Option('o', "optimization", Required = false, HelpText = "size of map buffer zone for pathfinding")]
        public int MapOptimizationBuffer { get; set; } = 20;
        [Option('q', "quickmode", Required = false, HelpText = "No helis, just go straight to target from nearest midpoint")]
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
        await trafficControl.JoinTeams(options.NumTeams, options.GameId);
        await waitForPlayingStatusAsync(trafficControl);

        trafficControl.FlyHelisToTargets();
        trafficControl.DriveRoversToTargets(heuristic, options.MapOptimizationBuffer);

        while (true)
        { }
    }
    private static async Task waitForPlayingStatusAsync(TrafficControlService trafficControl)
    {
        while (await trafficControl.CheckStatus() != TrafficControlService.GameStatus.Playing) ;
    }
}