using CommandLine;

internal partial class Program
{
    public class Options
    {
        [Option('t', "teams", Required = false, HelpText = "Number of rovers to drive")]
        public int NumTeams { get; set; } = 1;

        [Option('m', "maxteams", Required = false, HelpText = "Number of teams to join max")]
        public int MaxTeams { get; set; } = 50;

        [Option('g', "game", Required = false, HelpText = "Game ID")]
        public string GameId { get; set; } = "n";

        [Option('u', "url", Required = false, HelpText = "URL of game server")]
        public string Url { get; set; } = "https://snow-rover.azurewebsites.net/";

        //public string Url { get; set; } = "http://192.168.1.116/";
        //public string Url { get; set; } = "https://localhost:7287/";

        [Option(
            'e',
            "heuristic",
            Required = false,
            HelpText = "Heuristic to use (manhattan, euclidean)"
        )]
        public string Heuristic { get; set; } = "manhattan";

        [Option(
            'o',
            "optimization",
            Required = false,
            HelpText = "size of map buffer zone for pathfinding"
        )]
        public int MapOptimizationBuffer { get; set; } = 20;

        [Option('s', "straight", Required = false, HelpText = "Incentivize straight paths")]
        public bool StraightPath { get; set; } = false;

        [Option(
            'q',
            "quickmode",
            Required = false,
            HelpText = "No helis, for when battery doesnt matter."
        )]
        public bool QuickMode { get; set; } = false;
    }
}
