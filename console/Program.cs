using Roverlib.Services;
using Roverlib.Utils;

HttpClient client = new HttpClient()
{
    BaseAddress = new Uri("https://snow-rover.azurewebsites.net/")
    // BaseAddress = new Uri("https://localhost:64793/")
};
int NUM_TEAMS = 10;
string GAME_ID = "t";
try
{
    //NUM_TEAMS = args.Where(a => a.StartsWith("-t")).Select(a => int.Parse(a.Substring(2))).FirstOrDefault();
}
catch { }
//GAME_ID = args.Where(a => a.StartsWith("-g")).Select(a => a.Substring(2)).FirstOrDefault() ?? "b";
//string flightPattern = args.Where(a => a.StartsWith("-f")).Select(a => a.Substring(2)).FirstOrDefault() ?? "circle";
var trafficControl = new TrafficControlService(client);

// var map = MapReader.ReadMap("../maps/map01.json");
// var start = MapReader.FindPath(map, (0, 0), (50, 50));

await trafficControl.JoinTeams(NUM_TEAMS, GAME_ID);

await waitForPlayingStatusAsync(trafficControl);

trafficControl.FlyHeliFormation(out var source, formation: "spiral");
pathFindRoversAsync(trafficControl, source);

while (true)
{ }



static async Task waitForPlayingStatusAsync(TrafficControlService trafficControl)
{
    while (await trafficControl.CheckStatus() != TrafficControlService.GameStatus.Playing) ;
}



static async Task pathFindRoversAsync(TrafficControlService trafficControl, CancellationTokenSource source)
{
    var path = new List<(int, int)>();
    while (path.Count == 0)
    {
        foreach (var team in trafficControl.Teams)
        {
            var map = trafficControl.GameBoard.VisitedNeighbors.ToDictionary(k => (k.Value.X, k.Value.Y), v => v.Value.Difficulty);
            path = MapReader.FindPath(map, team.Rover.Location, trafficControl.GameBoard.Target);
            Thread.Sleep(3000);
            if (path.Count > 0)
            {
                var pathQueue = path.ToQueue();
                pathQueue.Dequeue();
                team.MoveRoverAlongPathAsync(pathQueue);
                source.Cancel();
                break;
            }
            //team.StepRoverTowardPointAsync(trafficControl.GameBoard.Target.X, trafficControl.GameBoard.Target.Y);
        }
    }
}



