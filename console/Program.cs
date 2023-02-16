using Roverlib.Services;
using Roverlib.Utils;

HttpClient client = new HttpClient()
{
    // BaseAddress = new Uri("https://snow-rover.azurewebsites.net/")
    BaseAddress = new Uri("https://localhost:64793/")
};

const int NUM_TEAMS = 1;
const string GAME_ID = "a";
var trafficControl = new TrafficControlService(client);

await joinTeams(NUM_TEAMS, GAME_ID, trafficControl);
var center = trafficControl.GameBoard.Target;
var radius = Math.Min(trafficControl.GameBoard.Width / 2, trafficControl.GameBoard.Height / 2);
await waitForPlayingStatusAsync(trafficControl);

//Task.Run(() => breathingCircle(NUM_TEAMS, trafficControl, center, radius));
Task.Run(() => sendHelisToTarget(trafficControl));
pathFindRoversAsync(trafficControl);

while (true)
{ }


static async Task joinTeams(int numTeams, string gameId, TrafficControlService trafficControl)
{
    for (int i = 0; i < numTeams; i++)
    {
        await trafficControl.JoinNewGame($"{i}", gameId);
    }
}

static async Task waitForPlayingStatusAsync(TrafficControlService trafficControl)
{
    while (await trafficControl.CheckStatus() != TrafficControlService.GameStatus.Playing) ;
}

static void moveHeliSwarmToPoints(TrafficControlService trafficControl, List<(int X, int Y)> flightPattern)
{
    List<Task> moveHelis = new List<Task>();
    for (int i = 0; i < trafficControl.Teams.Count; i++)
    {
        var team = trafficControl.Teams[i];
        var circlePos = flightPattern[i];
        var task = team.MoveHeliToPointAsync(circlePos);
        moveHelis.Add(task);
    }
    try
    {
        Task.WaitAll(moveHelis.ToArray());
    }
    catch { }
}

static async Task pathFindRoversAsync(TrafficControlService trafficControl)
{
    var path = new List<(int, int)>();
    while (path.Count == 0)
    {
        foreach (var team in trafficControl.Teams)
        {
            path = PathFinder.FindPathAStar(trafficControl.GameBoard.VisitedNeighbors, team.Rover.Location, trafficControl.GameBoard.Target);
            Thread.Sleep(3000);
            // team.StepRoverTowardPointAsync(trafficControl.GameBoard.Target.X, trafficControl.GameBoard.Target.Y);
        }
    }
    foreach (var team in trafficControl.Teams)
    {
        team.MoveRoverAlongPathAsync(path.ToQueue());
    }
}

static void breathingCircle(int NUM_TEAMS, TrafficControlService trafficControl, (int X, int Y) center, int radius)
{
    var helicircle = HeliPatterns.GenerateCircle(center, radius, NUM_TEAMS);
    var rotation = HeliPatterns.RotateList(helicircle, 0);
    for (int i = 5; i < 100; i++)
    {
        moveHeliSwarmToPoints(trafficControl, rotation);
        rotation = HeliPatterns.RotateList(rotation, i);
    }
}

static void sendHelisToTarget(TrafficControlService trafficControl)
{
    foreach (var team in trafficControl.Teams)
    {
        team.MoveHeliToPointAsync(trafficControl.GameBoard.Target);
    }
}

static void clockHand(int NUM_TEAMS, TrafficControlService trafficControl, (int X, int Y) center, int radius)
{
    for (int i = 1; i < 360; i++)
    {
        var flightPlan = HeliPatterns.GenerateClockHand(center, radius, i, NUM_TEAMS);
        moveHeliSwarmToPoints(trafficControl, flightPlan);
    }
}