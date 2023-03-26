using System.Net.Http.Json;
using Roverlib.Models;
using Roverlib.Models.Responses;
using Roverlib.Utils;

namespace Roverlib.Services;

public delegate void NotifyNeighborsDelegate(NewNeighborsEventArgs args);

public partial class TrafficControlService
{
    private readonly HttpClient client;
    private Location center = new(0, 0);
    private int radius;
    public List<RoverTeam> Teams { get; private set; } = new();
    public List<RoverTeam> ReconTeams { get; private set; } = new();
    public Board GameBoard { get; set; }

    public List<Location> TargetRoute { get; set; } = new();

    public EventHandler GameWonEvent { get; set; }

    public TrafficControlService(HttpClient client)
    {
        this.client = client;
        Teams = new();
    }

    public async Task<RoverTeam> JoinNewGame(string? name, string gameid)
    {
        var result = await client.GetAsync(
            $"/Game/Join?gameId={gameid}&name={name ?? NameGenerator.Generate()}"
        );
        if (result.IsSuccessStatusCode)
        {
            var res = await result.Content.ReadFromJsonAsync<JoinResponse>();
            if (GameBoard == null)
                GameBoard = new Board(res);
            if (TargetRoute.Count == 0)
                TargetRoute = new List<Location>(
                    TravelingSalesman.GetShortestRoute(GameBoard.Targets)
                );
            var newGame = new RoverTeam(name, gameid, res, client);
            newGame.NotifyGameManager += new NotifyNeighborsDelegate(onNewNeighbors);
            center = GameBoard.Targets[0];
            return newGame;
        }
        else
        {
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
            if (res.Detail.Contains("TooManyRequests"))
            {
                Console.WriteLine("Too many requests, waiting 1 seconds");
                await Task.Delay(1000);
                return await JoinNewGame(name, gameid);
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

    public async Task JoinUntilClose(string gameId, int numToDrive, int maxTeams = 50)
    { //Initialize game by joining one team
        var teams = new List<RoverTeam>();
        if (GameBoard == null)
        {
            teams.Add(await JoinNewGame(NameGenerator.Generate(), gameId));
        }

        //Figure out best starting target
        var bestStartingTarget = TargetRoute[0];

        //Join teams until there is a rover close to the best starting target
        while (!IsThereARoverCloseToTarget(teams, bestStartingTarget))
        {
            if (teams.Count >= maxTeams)
            {
                break;
            }
            var team = await JoinNewGame(NameGenerator.Generate(), gameId);
            teams.Add(team);
        }

        var closestRovers = OrderByDistanceToTarget(teams, bestStartingTarget);

        var roverVentureurs = closestRovers.Take(numToDrive).ToList();
        roverVentureurs.ForEach(
            x => x.Rover.WinEvent += (s, e) => GameWonEvent?.Invoke(this, EventArgs.Empty)
        );
        Teams.AddRange(roverVentureurs);

        //Add the rest for heli recon later
        ReconTeams.AddRange(teams);
    }

    private List<RoverTeam> OrderByDistanceToTarget(List<RoverTeam> teams, Location bestTarget)
    {
        var teamDistances = new Dictionary<RoverTeam, int>();
        foreach (var team in teams)
        {
            var distance = PathUtils.EuclideanDistance(team.Rover.Location, bestTarget);
            teamDistances.Add(team, distance);
        }
        return teamDistances.OrderBy(x => x.Value).Select(x => x.Key).ToList();
    }

    private void onNewNeighbors(NewNeighborsEventArgs args)
    {
        foreach (var n in args.Neighbors)
        {
            GameBoard.VisitedNeighbors.TryAdd(n.HashToLong(), n);
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

    // HELIS

    public void FlyHelisToTargets()
    {
        foreach (var team in Teams)
        {
            var task = Task.Run(() => team.Heli.FlyToTargets(GameBoard.Targets));
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
            var team = await JoinNewGame($"Recon{ReconTeams.Count}", Teams.First().GameId);
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
        List<RoverTeam> teams,
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
                PathUtils.ManhattanDistance(team.Rover.Location, target)
                <= distanceToEdge + minDistance
            )
            {
                return true;
            }
        }
        return false;
    }
}
