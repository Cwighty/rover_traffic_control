using Roverlib.Services;
using Roverlib.Utils;

HttpClient client = new HttpClient()
{
    BaseAddress = new Uri("https://snow-rover.azurewebsites.net/")
    // BaseAddress = new Uri("https://localhost:64793/")
};

int NUM_TEAMS = args.Where(a => a.StartsWith("-t")).Select(a => int.Parse(a.Substring(2))).FirstOrDefault();
string GAME_ID = args.Where(a => a.StartsWith("-g")).Select(a => a.Substring(2)).FirstOrDefault() ?? "a";
string flightPattern = args.Where(a => a.StartsWith("-f")).Select(a => a.Substring(2)).FirstOrDefault() ?? "circle";
var trafficControl = new TrafficControlService(client);

await trafficControl.JoinTeams(NUM_TEAMS, GAME_ID);

await waitForPlayingStatusAsync(trafficControl);

trafficControl.FlyHeliFormation(flightPattern);
pathFindRoversAsync(trafficControl);

while (true)
{ }



static async Task waitForPlayingStatusAsync(TrafficControlService trafficControl)
{
    while (await trafficControl.CheckStatus() != TrafficControlService.GameStatus.Playing) ;
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
            //team.StepRoverTowardPointAsync(trafficControl.GameBoard.Target.X, trafficControl.GameBoard.Target.Y);
        }
    }
    foreach (var team in trafficControl.Teams)
    {
        team.MoveRoverAlongPathAsync(path.ToQueue());
    }
}



