using System.Net.Http.Json;
using System.Text.Json;
using Roverlib.Models;
using Roverlib.Models.Responses;
using Roverlib.Utils;

namespace Roverlib.Services;

public delegate void NotifyNeighborsDelegate(NewNeighborsEventArgs args);

public partial class TrafficControlService
{
    private readonly HttpClient client;
    private readonly bool quickMode;
    private Location center = new(0, 0);
    private int radius;
    public List<RoverTeam> Teams { get; private set; } = new();
    public List<RoverTeam> ReconTeams { get; private set; } = new();
    public Board GameBoard { get; set; }
    public string GameId { get; set; }

    public List<Location> TargetRoute { get; set; } = new();

    public EventHandler GameWonEvent { get; set; }

    public TrafficControlService(HttpClient client, bool quickMode = false)
    {
        this.client = client;
        this.quickMode = quickMode;
        Teams = new();
    }

    public async Task InitializeGame(string gameId, string? name)
    {
        var res = await JoinNewGameResponse("probe", gameId);
        if (GameBoard == null)
            GameBoard = new Board(res);
        if (TargetRoute.Count == 0)
            TargetRoute = new List<Location>(
                TravelingSalesman.GetBestRoute(GameBoard.Targets, GameBoard.Width, GameBoard.Height)
            );
        center = GameBoard.Targets[0];
        GameId = gameId;
    }

    public async Task<RoverTeam> JoinNewGame(string? name, string gameid)
    {
        try
        {
            var result = await JoinNewGameResponse(name, gameid);
            var newGame = new RoverTeam(name, result, client);
            newGame.NotifyGameManager += new NotifyNeighborsDelegate(onNewNeighbors);
            return newGame;
        }
        catch
        {
            return await JoinNewGame(name, gameid);
        }
    }

    public async Task<JoinResponse> JoinNewGameResponse(string? name, string gameid)
    {
        var result = await client.GetAsync(
            $"/Game/Join?gameId={gameid}&name={name ?? NameGenerator.Generate()}"
        );
        if (result.IsSuccessStatusCode)
        {
            var res = await result.Content.ReadFromJsonAsync<JoinResponse>();

            return res;
        }
        else
        {
            if (result.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                Console.WriteLine("Too many requests, waiting 1 seconds");
                await Task.Delay(1000);
                return await JoinNewGameResponse(name, gameid);
            }
            var res = new ProblemDetail();
            try
            {
                res = await result.Content.ReadFromJsonAsync<ProblemDetail>();
            }
            catch (Exception e)
            {
                res.Title = result.ReasonPhrase;
                res.Detail = result.StatusCode.ToString();
            }
            Console.WriteLine(res.Detail);
            throw new ProblemDetailException(res);
        }
    }

    public async Task JoinTeams(int numTeams, string gameId, string? name = null)
    {
        var roverTeams = new List<RoverTeam>();
        for (int i = 0; i < numTeams; i++)
        {
            var team = await JoinNewGame(name, gameId);
            team.Rover.WinEvent += (s, e) => GameWonEvent?.Invoke(this, EventArgs.Empty);
            Teams.Add(team);
        }
        radius = Math.Min(GameBoard.Width / 2, GameBoard.Height / 2);
    }

    public async Task JoinUntilClose(
        string gameId,
        int numToDrive,
        int maxTeams = 50,
        string? name = null
    )
    {
        var responses = new List<JoinResponse>();
        //Figure out best starting target
        var bestStartingTarget = TargetRoute[0];

        //Join teams until there is a rover close to the best starting target
        while (!IsThereARoverCloseToTarget(responses, bestStartingTarget))
        {
            if (responses.Count >= maxTeams)
            {
                break;
            }
            var res = await JoinNewGameResponse(name ?? NameGenerator.Generate(), gameId);
            responses.Add(res);
        }

        var closestRovers = OrderByDistanceToTarget(responses, bestStartingTarget);
        TargetRoute.RemoveAt(0);

        //export each team to a file
        ExportTeamsToFile(closestRovers.Take(3).ToList());
    }

    private static void ExportTeamsToFile(List<JoinResponse> responses)
    {
        int i = 0;
        foreach (var res in responses)
        {
            i++;
            var filename = $"../joined/{i}_{res.token}.json";
            var json = JsonSerializer.Serialize(res);
            File.WriteAllText(filename, json);
        }
    }

    private List<JoinResponse> OrderByDistanceToTarget(
        List<JoinResponse> responses,
        Location bestTarget
    )
    {
        var teamDistances = new Dictionary<JoinResponse, int>();
        foreach (var res in responses)
        {
            var loc = new Location(res.startingX, res.startingY);
            var distance = PathUtils.EuclideanDistance(loc, bestTarget);
            teamDistances.Add(res, distance);
        }
        return teamDistances.OrderBy(x => x.Value).Select(x => x.Key).ToList();
    }

    private void onNewNeighbors(NewNeighborsEventArgs args)
    {
        if (!quickMode)
        {
            foreach (var n in args.Neighbors)
            {
                GameBoard.VisitedNeighbors.TryAdd(n.HashToLong(), n);
            }
        }
    }

    public async Task<GameStatus> CheckStatus()
    {
        if (Teams.Count == 0)
            return GameStatus.Invalid;
        var res = await client.GetAsync($"/Game/Status?token={Teams[0].Token}");
        if (res.IsSuccessStatusCode)
        {
            try
            {
                var result = await res.Content.ReadAsStringAsync();
                if (result.Contains("Playing"))
                    return GameStatus.Playing;
                if (result.Contains("Joining"))
                    return GameStatus.Joining;
                return GameStatus.Invalid;
            }
            catch (Exception e)
            {
                return GameStatus.Invalid;
            }
        }
        return GameStatus.Invalid;
    }

    public void CancelAll()
    {
        foreach (var team in Teams)
        {
            team.Heli.CancelFlight();
            team.Rover.CancelDrive();
        }
    }

    public void DriveRoversToTargets(Func<(int, int), (int, int), int> heuristic, int optBuffer)
    {
        foreach (var team in Teams)
        {
            //Include the rovers current location as a target
            var task = Task.Run(
                () =>
                    team.Rover.DriveToTargets(
                        GameBoard.VisitedNeighbors,
                        TargetRoute,
                        heuristic,
                        optBuffer
                    )
            );
        }
    }

    public void DriveRoversStraightToTargets()
    {
        foreach (var team in Teams)
        {
            //Include the rovers current location as a target
            var task = Task.Run(() => team.Rover.DriveStraightToTargetsAsync(TargetRoute));
        }
    }

    // HELIS

    public void FlyHelisToTargets()
    {
        foreach (var team in Teams)
        {
            var task = Task.Run(() => team.Heli.FlyToTargets(TargetRoute));
        }
    }

    public async Task FlyHeliReconMission()
    {
        await HeliScanAsync(5);
    }

    private async Task HeliScanAsync(int maxHelis)
    {
        while (ReconTeams.Count < maxHelis)
        {
            var team = await JoinNewGame($"Recon{ReconTeams.Count}", GameId);
            ReconTeams.Add(team);
        }
        var tileLocations = new List<Location>();
        foreach (var tile in GameBoard.LowResMap)
        {
            var tileWidth = tile.upperRightX - tile.lowerLeftX;
            var tileHeight = tile.upperRightY - tile.lowerLeftY;
            //make a list of locatins in center of each tile
            var center = new Location(
                tile.lowerLeftX + (tileWidth / 2),
                tile.lowerLeftY + (tileHeight / 2)
            );
            tileLocations.Add(center);
        }

        //split up the locations for each heli
        var numTeams = ReconTeams.Count < maxHelis ? ReconTeams.Count : maxHelis;
        var total = tileLocations.Count;
        var perTeam = total / numTeams;
        //group the locations into lists
        var teamLocations = new List<List<Location>>();
        for (int i = 0; i < numTeams; i++)
        {
            teamLocations.Add(tileLocations.GetRange(i * perTeam, perTeam));
        }
        //move the helis to the locations
        foreach (var team in ReconTeams.Take(numTeams))
        {
            // assign helis to nearest team location
            var heliLocation = team.Heli.Location;
            var nearestLocations = GetNearestLocations(heliLocation, teamLocations);
            var task = team.Heli.FlyToReconPoints(nearestLocations);
            teamLocations.Remove(nearestLocations);
        }
    }

    private List<Location> GetNearestLocations(
        Location heliLocation,
        List<List<Location>> teamLocations
    )
    {
        var nearestLocations = new List<Location>();
        var nearestDistance = int.MaxValue;
        foreach (var locations in teamLocations)
        {
            var distance = PathUtils.EuclideanDistance(heliLocation, locations.First());
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestLocations = locations;
            }
        }
        return nearestLocations;
    }

    private bool IsThereARoverCloseToTarget(
        List<JoinResponse> teams,
        Location target,
        int minDistance = 5
    )
    {
        var distanceToEdge = Math.Min(
            Math.Min(target.X, GameBoard.Width - target.X),
            Math.Min(target.Y, GameBoard.Height - target.Y)
        );
        foreach (var team in teams)
        {
            if (
                PathUtils.ManhattanDistance(new Location(team.startingX, team.startingY), target)
                <= distanceToEdge + minDistance
            )
            {
                return true;
            }
        }
        return false;
    }

    public void ImportTeams(string name)
    {
        while (Teams.Count < 1)
        {
            //foreach through directory
            foreach (var file in Directory.EnumerateFiles("../joined").OrderBy(f => f))
            {
                try
                {
                    using (
                        var stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.None)
                    )
                    {
                        //deserialize the file
                        var res = JsonSerializer.Deserialize<JoinResponse>(stream);
                        //add the team to the list
                        var team = new RoverTeam(name, res, client);
                        team.NotifyGameManager += new NotifyNeighborsDelegate(onNewNeighbors);
                        team.Rover.WinEvent += (s, e) =>
                            GameWonEvent?.Invoke(this, EventArgs.Empty);
                        Teams.Add(team);
                        Console.WriteLine($"Imported team {team.Name}");
                    }
                    File.Delete(file);
                    break;
                }
                catch (IOException e)
                {
                    continue;
                }
            }
        }

        if (name.Contains("smart"))
        {
            foreach (var file in Directory.EnumerateFiles("../joined").Reverse())
            {
                try
                {
                    var res = JsonSerializer.Deserialize<JoinResponse>(File.ReadAllText(file));
                    File.Delete(file);
                    var team = new RoverTeam(name, res, client);
                    ReconTeams.Add(team);
                }
                catch (IOException e)
                {
                    continue;
                }
            }
        }
    }
}
