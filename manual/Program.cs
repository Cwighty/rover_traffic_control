using CommandLine;
using Roverlib.Models;
using Roverlib.Models.Responses;
using Roverlib.Services;
using Roverlib.Utils;

public class Program
{
    public class Options
    {
        [Option('g', "game", Required = false, HelpText = "Game ID")]
        public string GameId { get; set; } = "bz";

        [Option('u', "url", Required = false, HelpText = "URL of game server")]
        public string Url { get; set; } = "https://snow-rover.azurewebsites.net/";
        //public string Url { get; set; } = "http://192.168.1.116/";
        //public string Url { get; set; } = "https://localhost:7287/";
    }

    private static async Task Main(string[] args)
    {
        var options = Parser.Default.ParseArguments<Options>(args).Value;
        //join the game
        var client = new HttpClient() { BaseAddress = new Uri(options.Url) };
        var trafficControl = new TrafficControlService(client);
        var res = await trafficControl.InitializeGame(options.GameId, "yeeeeeeee");
        var team = new RoverTeam("Caleb", res, client);
        trafficControl.Teams.Add(team);
        var rover = team.Rover;

        var targets = trafficControl.GameBoard.Targets;

        while (await trafficControl.CheckStatus() != GameStatus.Playing) { }
        while (true)
        {
            //if rover location is a target, remove it
            if (targets.Any(t => t.X == rover.Location.X && t.Y == rover.Location.Y))
            {
                targets.Remove(
                    targets.First(t => t.X == rover.Location.X && t.Y == rover.Location.Y)
                );
            }
            // display the targets
            DisplayMessage(targets, rover);

            // check for input availability before reading a key
            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                switch (keyInfo.Key)
                {
                    case ConsoleKey.UpArrow:
                        await rover.MoveAsync(Direction.Forward);
                        break;
                    case ConsoleKey.DownArrow:
                        await rover.MoveAsync(Direction.Reverse);
                        break;
                    case ConsoleKey.LeftArrow:
                        await rover.MoveAsync(Direction.Left);
                        break;
                    case ConsoleKey.RightArrow:
                        await rover.MoveAsync(Direction.Right);
                        break;
                    case ConsoleKey.Spacebar:
                        break;
                }

                // Clear any remaining input from the buffer
                while (Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                }
            }

        }
    }

    private static void DisplayMessage(List<Location> targets, PerserveranceRover rover)
    {
        Console.Clear();

        Console.WriteLine("Battery: " + rover.Battery);
        Console.WriteLine("Targets:");
        foreach (var target in targets)
        {
            Console.Write($"({target.X}, {target.Y})");
        }
        Console.WriteLine();

        Console.Write("Rover: ");
        Console.WriteLine($"({rover.Location.X}, {rover.Location.Y})");

        // get the next target
        var nextTarget = targets
            .OrderBy(
                t => PathUtils.EuclideanDistance((t.X, t.Y), (rover.Location.X, rover.Location.Y))
            )
            .First();
        Console.WriteLine($"Next Closest Target: ({nextTarget.X}, {nextTarget.Y})");
    }
}
