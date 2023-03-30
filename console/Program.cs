using CommandLine;
using Roverlib.Models;
using Roverlib.Services;
using Roverlib.Utils;

internal partial class Program
{
    public static bool IsMapCached { get; private set; } = false;

    private static async Task Main(string[] args)
    {
        var gameOptions = Parser.Default.ParseArguments<Options>(args).Value;
        Func<(int, int), (int, int), int> heuristic = gameOptions.Heuristic switch
        {
            "manhattan" => PathUtils.ManhattanDistance,
            "euclidean" => PathUtils.EuclideanDistance,
            _ => PathUtils.ManhattanDistance
        };
        Console.WriteLine($"Game ID: {gameOptions.GameId}");
        if (gameOptions.StraightPath)
            PathUtils.StraightIncentive = 500;

        HttpClient client = new HttpClient() { BaseAddress = new Uri(gameOptions.Url) };
        var trafficControl = new TrafficControlService(client, gameOptions.QuickMode);
        await trafficControl.InitializeGame(gameId: gameOptions.GameId, name: gameOptions.Name);
        trafficControl.GameWonEvent += async (sender, e) =>
        {
            await ReconAndCacheMap(trafficControl);
        };

        string filePath = CheckForCachedMap(trafficControl);

        //Check if joined directory empty
        if (gameOptions.Name.Contains("joiner"))
        {
            //if empty, join game
            Directory.CreateDirectory("../joined");
            await trafficControl.JoinUntilClose(
                gameOptions.GameId,
                gameOptions.NumTeams,
                gameOptions.MaxTeams
            );
        }
        else
        {
            while (true)
            {
                //check directory exists
                if (!Directory.Exists("../joined"))
                {
                    Console.WriteLine(
                        "Joined directory not found, waiting for joiner to join game"
                    );
                    Thread.Sleep(1000);
                    continue;
                }
                if (Directory.GetFiles("../joined").Length > 0)
                {
                    break;
                }
                Thread.Sleep(1000);
            }
            Thread.Sleep(1000);
            trafficControl.ImportTeams(gameOptions.Name);
            trafficControl.TargetRoute.RemoveAt(0);
        }

        await waitForPlayingStatusAsync(trafficControl);
        Console.WriteLine("Game started");
        //delete joined directory

        if (gameOptions.QuickMode)
        {
            trafficControl.DriveRoversStraightToTargets();
        }
        else
        {
            if (!IsMapCached)
                trafficControl.FlyHelisToTargets();
            trafficControl.DriveRoversToTargets(heuristic, gameOptions.MapOptimizationBuffer);
        }

        while (true)
        {
            await UpdateCachedMap(gameOptions, trafficControl, filePath);
            await Task.Delay(8000);
        }
    }

    private static string CheckForCachedMap(TrafficControlService trafficControl)
    {
        var filePath =
            $"../maps/{MapHelper.GetFileNameFromMap(trafficControl.GameBoard.LowResMap)}";
        if (File.Exists(filePath))
        {
            Console.WriteLine($"Map file path: {filePath}");
            InitializeMapWithCachedFile(trafficControl, filePath);
        }
        else
        {
            Console.WriteLine($"No Map file path: {filePath}");
        }

        return filePath;
    }

    private static async Task UpdateCachedMap(
        Options gameOptions,
        TrafficControlService trafficControl,
        string filePath
    )
    {
        if (!gameOptions.QuickMode)
        {
            MapHelper.WriteMapToCSV(
                trafficControl.GameBoard.VisitedNeighbors,
                filePath,
                trafficControl.GameBoard.LowResMap
            );
        }
    }

    private static void InitalizeMapWithLowResMap(TrafficControlService trafficControl)
    {
        trafficControl.GameBoard.VisitedNeighbors = MapHelper.InitializeDefaultMap(
            trafficControl.GameBoard.LowResMap
        );
    }

    private static void InitializeMapWithCachedFile(
        TrafficControlService trafficControl,
        string filePath
    )
    {
        IsMapCached = true;
        trafficControl.GameBoard.VisitedNeighbors = MapHelper.ReadMapFromCSV(filePath);
    }

    private static async Task ReconAndCacheMap(TrafficControlService trafficControl)
    {
        if (IsMapCached)
            return;
        trafficControl.CancelAll();
        await trafficControl.FlyHeliReconMission();
    }

    private static async Task waitForPlayingStatusAsync(TrafficControlService trafficControl)
    {
        while (await trafficControl.CheckStatus() != GameStatus.Playing)
            ;
    }
}
