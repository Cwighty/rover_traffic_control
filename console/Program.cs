using CommandLine;
using Roverlib.Models;
using Roverlib.Services;

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
        if (gameOptions.StraightPath)
            PathUtils.StraightIncentive = 100;

        HttpClient client = new HttpClient() { BaseAddress = new Uri(gameOptions.Url) };
        var trafficControl = new TrafficControlService(client);

        trafficControl.GameWonEvent += async (sender, e) =>
        {
            await ReconAndCacheMap(trafficControl);
        };

        await trafficControl.JoinUntilClose(
            gameOptions.GameId,
            gameOptions.NumTeams,
            gameOptions.MaxTeams
        );

        var filePath =
            $"../maps/{MapHelper.GetFileNameFromMap(trafficControl.GameBoard.LowResMap)}";
        Console.WriteLine($"Map file path: {filePath}");
        await waitForPlayingStatusAsync(trafficControl);

        if (File.Exists(filePath))
        {
            InitializeMapWithCachedFile(trafficControl, filePath);
        }
        else
        {
            if (gameOptions.QuickMode)
            {
                if (!IsMapCached)
                    InitalizeMapWithLowResMap(trafficControl);
                //Dont fly any helis to scout
            }
            else
            {
                // fly helis to scout
                trafficControl.FlyHelisToTargets();
            }
        }

        trafficControl.DriveRoversToTargets(heuristic, gameOptions.MapOptimizationBuffer);

        while (true)
        {
            await UpdateCachedMap(gameOptions, trafficControl, filePath);
            await Task.Delay(1000);
        }
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
