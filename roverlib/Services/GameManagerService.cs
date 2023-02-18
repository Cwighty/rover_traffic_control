using System.Net.Http.Json;
using Roverlib.Models;
using Roverlib.Models.Responses;

namespace Roverlib.Services;
public delegate void NotifyNeighborsDelegate(NewNeighborsEventArgs args);
public partial class TrafficControlService
{
    private readonly HttpClient client;
    public List<RoverTeam> Teams { get; set; }
    public Board GameBoard { get; set; }


    public TrafficControlService(HttpClient client)
    {
        this.client = client;
        Teams = new();
    }

    public async Task JoinNewGame(string name, string gameid)
    {
        var result = await client.GetAsync($"/Game/Join?gameId={gameid}&name={GenerateRandomName()}");
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

    static string GenerateRandomName(){
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
        return $"{letterAdjective} {animal}";
    }
}


