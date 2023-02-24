using System.Net.Http.Json;
using Roverlib.Models;
using Roverlib.Models.Responses;

namespace Roverlib.Services;
public delegate void NotifyNeighborsDelegate(NewNeighborsEventArgs args);
public partial class TrafficControlService
{
    private readonly HttpClient client;
    private (int X, int Y) center;
    private int radius;

    public List<RoverTeam> Teams { get; set; }
    public Board GameBoard { get; set; }


    public TrafficControlService(HttpClient client)
    {
        this.client = client;
        Teams = new();
    }

    async Task joinNewGame(string name, string gameid)
    {
        var result = await client.GetAsync($"/Game/Join?gameId={gameid}&name={generateRandomName()}");
        if (result.IsSuccessStatusCode)
        {
            var res = await result.Content.ReadFromJsonAsync<JoinResponse>();
            if (GameBoard == null)
                GameBoard = new Board(res);
            var newGame = new RoverTeam(name, gameid, res, client);
            newGame.NotifyGameManager += new NotifyNeighborsDelegate(onNewNeighbors);
            Teams.Add(newGame);
        }
        else
        {
            var res = await result.Content.ReadFromJsonAsync<ProblemDetail>();
            throw new ProblemDetailException(res);
        }
    }


    public async Task JoinTeams(int numTeams, string gameId)
    {
        for (int i = 0; i < numTeams; i++)
        {
            await joinNewGame($"{i}", gameId);
        }
        center = GameBoard.Target;
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

    public void FlyHeliFormation(string formation = "circle")
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        var token = cts.Token;
        int numTeams = Teams.Count(); ;
        if (formation == "circle")
        {
            var tasks = new List<Task>();
            foreach (var team in Teams)
            {
                var task = team.MoveHeliToNearestAxisAsync(this);
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());
            Task.Run(() => breathingCircle(numTeams, center, radius, token));
        }
        else if (formation == "spiral")
        {
            Task.Run(() => spiral(numTeams, center, token));
        }
        else if (formation == "clock")
        {
            Task.Run(() => clockHand(numTeams, center, radius, token));
        }
        else if (formation == "target")
        {
            Task.Run(() => sendHelisToTarget(token));
        }
    }

    public void DriveRovers(Func<(int, int), (int, int), int> heuristic = null)
    {
        foreach (var team in Teams)
        {
            var t = Task.Run(() => team.MoveRoverToNearestAxisAsync(this));
        }
        var path = new List<(int, int)>();
        while (path.Count == 0)
        {
            var sent = new List<RoverTeam>();
            foreach (var team in Teams)
            {
                var map = GameBoard.VisitedNeighbors.ToDictionary(k => (k.Value.X, k.Value.Y), v => v.Value.Difficulty);
                path = PathFinder.FindPath(map, team.Rover.Location, GameBoard.Target, heuristic);
                Thread.Sleep(3000);
                if (path.Count > 0)
                {
                    var pathQueue = path.ToQueue();
                    pathQueue.Dequeue();
                    if (!team.HeliCancelSource.IsCancellationRequested)
                    {
                        var t = Task.Run(() => team.MoveRoverAlongPathAsync(pathQueue));
                    }
                    team.CancelHeli();
                }
            }
        }
    }
    void breathingCircle(int NUM_TEAMS, (int X, int Y) center, int radius, CancellationToken token)
    {
        var helicircle = HeliPatterns.GenerateCircle(center, radius, NUM_TEAMS);
        var rotation = HeliPatterns.RotateList(helicircle, 0);

        var target = GameBoard.Target;
        var startingPoints = new List<(int X, int Y)>();
        for (int j = 0; j < Teams.Count(); j++)
        {
            startingPoints.Add(target);
        }
        moveHeliSwarmToPoints(startingPoints);
        for (int i = 5; i < 100; i++)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }
            moveHeliSwarmToPoints(rotation);
            rotation = HeliPatterns.RotateList(rotation, i);
        }
    }

    void sendHelisToTarget(CancellationToken token)
    {
        foreach (var team in Teams)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }
            var t = Task.Run(() => team.MoveHeliToPointAsync(GameBoard.Target));
        }
    }

    void clockHand(int NUM_TEAMS, (int X, int Y) center, int radius, CancellationToken token)
    {
        for (int i = 1; i < 360; i++)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }
            var flightPlan = HeliPatterns.GenerateClockHand(center, radius, i, NUM_TEAMS);
            moveHeliSwarmToPoints(flightPlan);
        }
    }

    void spiral(int NUM_TEAMS, (int X, int Y) center, CancellationToken token)
    {
        for (int i = 1; i < 360; i += 10)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }
            var rotation = HeliPatterns.GeneratePhyllotaxisSpiral(center, i, NUM_TEAMS, distanceBetween: 50);
            moveHeliSwarmToPoints(rotation);
        }
    }
    void moveHeliSwarmToPoints(List<(int X, int Y)> flightPattern)
    {
        List<Task> moveHelis = new List<Task>();
        for (int i = 0; i < Teams.Count; i++)
        {
            var team = Teams[i];
            var circlePos = flightPattern[i];
            if (!team.HeliCancelSource.IsCancellationRequested)
            {
                var task = team.MoveHeliToPointAsync(circlePos);
                moveHelis.Add(task);
            }
        }
        try
        {

            Task.WaitAll(moveHelis.ToArray());
        }
        catch { }
    }
}


