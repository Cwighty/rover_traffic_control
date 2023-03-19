using System.Net.Http.Json;
using Roverlib.Models;
using Roverlib.Models.Responses;

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

    public EventHandler GameWonEvent { get; set; }

    public TrafficControlService(HttpClient client)
    {
        this.client = client;
        Teams = new();
    }

    public async Task<RoverTeam> JoinNewGame(string? name, string gameid)
    {
        var result = await client.GetAsync($"/Game/Join?gameId={gameid}&name={name ?? generateRandomName()}");
        if (result.IsSuccessStatusCode)
        {
            var res = await result.Content.ReadFromJsonAsync<JoinResponse>();
            if (GameBoard == null)
                GameBoard = new Board(res);
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

    private void onNewNeighbors(NewNeighborsEventArgs args)
    {
        foreach (var n in args.Neighbors)
        {
            GameBoard.VisitedNeighbors.TryAdd(n.HashToLong(), n);
        }
    }

    public async Task<GameStatus> CheckStatus()
    {
        if (Teams.Count == 0) return GameStatus.Invalid;
        var res = await client.GetAsync($"/Game/Status?token={Teams[0].Token}");
        if (res.IsSuccessStatusCode)
        {
            try
            {
                var result = await res.Content.ReadAsStringAsync();
                if (result.Contains("Playing")) return GameStatus.Playing;
                if (result.Contains("Joining")) return GameStatus.Joining;
                return GameStatus.Invalid;
            }
            catch (Exception e)
            {
                return GameStatus.Invalid;
            }
        }
        return GameStatus.Invalid;
    }
    public void FlyHelisToTargets()
    {
        foreach (var team in Teams)
        {
            var task = Task.Run(() => team.Heli.FlyToTargets(GameBoard.Targets));
        }
    }

    public void DriveRoversToTargets(Func<(int, int), (int, int), int> heuristic, int optBuffer)
    {
        foreach (var team in Teams)
        {
            var task = Task.Run(() => team.Rover.DriveToTargets(GameBoard.VisitedNeighbors, GameBoard.Targets, heuristic, optBuffer));
        }
    }


    public async Task FlyHeliReconMission()
    {
        HeliScanAsync(15);
    }

    private async Task<List<RoverTeam>> JoinReconHelis(int numHelis)
    {
        var roverTeams = new List<RoverTeam>();
        for (int i = 0; i < numHelis; i++)
        {
            var team = await JoinNewGame($"Recon{i}", Teams.First().GameId);
            roverTeams.Add(team);
        }
        return roverTeams;
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
            var center = new Location(tile.lowerLeftX + (tileWidth / 2), tile.lowerLeftY + (tileHeight / 2));
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

    private List<Location> GetNearestLocations(Location heliLocation, List<List<Location>> teamLocations)
    {
        var nearestLocations = new List<Location>();
        var nearestDistance = int.MaxValue;
        foreach (var locations in teamLocations)
        {
            var distance = GetDistance(heliLocation, locations.First());
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestLocations = locations;
            }
        }
        return nearestLocations;
    }

    private int GetDistance(Location heliLocation, Location location)
    {
        var x = heliLocation.X - location.X;
        var y = heliLocation.Y - location.Y;
        return (int)Math.Sqrt(x * x + y * y);
    }

    public void CancelAll()
    {
        foreach (var team in Teams)
        {
            team.Heli.CancelFlight();
            team.Rover.CancelDrive();
        }
    }

    public async Task JoinUntilClose(string gameId, int maxTeams = 50)
    {
        var teams = new List<RoverTeam>();
        if (GameBoard == null)
        {
            teams.Add(await JoinNewGame(generateRandomName(), gameId));
        }
        while (!IsThereARoverCloseToTarget(teams))
        {
            if (teams.Count >= maxTeams)
            {
                break;
            }
            var team = await JoinNewGame(generateRandomName(), gameId);
            teams.Add(team);
        }
        var closestRovers = OrderByDistanceToTarget(teams);
        var ventureurs = closestRovers.Take(1).ToList();
        ventureurs.ForEach(x => x.Rover.WinEvent += (s, e) => GameWonEvent?.Invoke(this, EventArgs.Empty));

        Teams.AddRange(ventureurs);
        ReconTeams.AddRange(teams);
    }

    private List<RoverTeam> OrderByDistanceToTarget(List<RoverTeam> teams)
    {
        var targets = GameBoard.Targets;
        var roverDistances = new List<(RoverTeam, int)>();
        foreach (var team in teams)
        {
            var rover = team.Rover;
            var roverLocation = rover.Location;
            foreach (var target in targets)
            {
                var distance = GetDistance(roverLocation, target);
                roverDistances.Add((team, distance));
            }
        }
        return roverDistances.OrderBy(x => x.Item2).Select(x => x.Item1).ToList();
    }

    private bool IsThereARoverCloseToTarget(List<RoverTeam> teams)
    {
        var targets = GameBoard.Targets;
        var closestDistance = GetClosestTargetDistanceToEdge();
        foreach (var team in teams)
        {
            var rover = team.Rover;
            var roverLocation = rover.Location;
            foreach (var target in targets)
            {
                var distance = GetDistance(roverLocation, target);
                if (distance <= closestDistance)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private double GetClosestTargetDistanceToEdge()
    {
        var targets = GameBoard.Targets;
        var mapWidth = GameBoard.Width;
        var mapHeight = GameBoard.Height;
        double minDistance = double.MaxValue;

        foreach (Location target in targets)
        {
            double distanceToLeftEdge = target.X;
            double distanceToRightEdge = mapWidth - target.X;
            double distanceToTopEdge = target.Y;
            double distanceToBottomEdge = mapHeight - target.Y;

            double minDistanceToEdge = Math.Min(Math.Min(distanceToLeftEdge, distanceToRightEdge), Math.Min(distanceToTopEdge, distanceToBottomEdge));

            if (minDistanceToEdge < minDistance)
            {
                minDistance = minDistanceToEdge;
            }
        }
        return minDistance;
    }

    static string generateRandomName()
    {
        var animals = new HashSet<string>{
            "Aardvark",
            "Albatross",
            "Alligator",
            "Alpaca",
            "Ant",
            "Anteater",
            "Antelope",
            "Ape",
            "Armadillo",
            "Donkey",
            "Baboon",
            "Badger",
            "Barracuda",
            "Bat",
            "Bear",
            "Beaver",
            "Bee",
            "Bison",
            "Boar",
            "Buffalo",
            "Butterfly",
            "Camel",
            "Capybara",
            "Caribou",
            "Cassowary",
            "Cat",
            "Caterpillar",
            "Cattle",
            "Chamois",
            "Cheetah",
            "Chicken",
            "Chimpanzee",
            "Chinchilla",
            "Chough",
            "Clam",
            "Cobra",
            "Cockroach",
            "Cod",
            "Cormorant",
            "Coyote",
            "Crab",
            "Crane",
            "Crocodile",
            "Crow",
            "Curlew",
            "Deer",
            "Dinosaur",
            "Dog",
            "Dogfish",
            "Dolphin",
            "Dotterel",
            "Dove",
            "Dragonfly",
            "Duck",
            "Dugong",
            "Dunlin",
            "Eagle",
            "Echidna",
            "Eel",
            "Eland",
            "Elephant",
            "Elk",
            "Emu",
            "Falcon",
            "Ferret",
            "Finch",
            "Fish",
            "Flamingo",
            "Fly",
            "Fox",
            "Frog",
            "Gaur",
            "Gazelle",
            "Gerbil",
            "Giraffe",
            "Gnat",
            "Gnu",
            "Goat",
            "Goldfinch",
            "Goldfish",
            "Goose",
            "Gorilla",
            "Goshawk",
            "Grasshopper",
            "Grouse",
            "Guanaco",
            "Gull",
            "Hamster",
            "Hare",
            "Hawk",
            "Hedgehog",
            "Heron",
            "Herring",
            "Hippopotamus",
            "Hornet",
            "Horse",
            "Human",
            "Hummingbird",
            "Hyena",
            "Ibex",
            "Ibis",
            "Jackal",
            "Jaguar",
            "Jay",
            "Jellyfish",
            "Kangaroo",
            "Kingfisher",
            "Koala",
            "Kookabura",
        };
        var letterAdjectives = new HashSet<string>{
            "Abundant",
            "Bountiful",
            "Coveted",
            "Desired",
            "Eager",
            "Fervent",
            "Gleeful",
            "Happy",
            "Joyful",
            "Keen",
            "Lively",
            "Merry",
            "Pleased",
            "Thrilled",
            "Willing",
            "Zealous",
            "Agreeable",
            "Amiable",
            "Blithe",
            "Bubbly",
            "Cheerful",
            "Companionable",
            "Congenial",
            "Delightful",
            "Droopy",
            "Eager",
            "Ecstatic",
            "Elated",
            "Enthusiastic",
            "Exuberant",
            "Festive",
            "Flippant",
            "Glad",
            "Gleeful",
            "Gracious",
            "Happy",
            "Jolly",
            "Jovial",
            "Joyous",
            "Inquisitive",
            "Interested",
            "Interesting",
            "Keen",
            "Kind",
            "Lively",
            "Loving",
            "Lucky",
            "Mischievous",
            "Mirthful",
            "Optimistic",
            "Peppy",
            "Quirky",
            "Relaxed",
            "Silly",
            "Smiling",
            "Sunny",
            "Tender",
            "Thrilled",
            "Tolerant",
            "Trusting",
            "Terrible",
            "Rowdy",
            "Rambunctious",
            "Raucous",
            "Zany",
            "Persnickety",
            "Yappy",
            "Wacky",
            "Vivacious",
            "Yawning"
        };

        var random = new Random();
        var animal = animals.ElementAt(random.Next(animals.Count));
        var letterAdjective = letterAdjectives.ElementAt(random.Next(letterAdjectives.Count));
        return $"{letterAdjective}{animal}";
    }
}


