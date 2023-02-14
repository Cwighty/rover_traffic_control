using Roverlib.Models.Responses;
using Roverlib.Services;

HttpClient client = new HttpClient()
{
    BaseAddress = new Uri("https://snow-rover.azurewebsites.net/")
};

var trafficControl = new TrafficControlService(client);

var helicircle = HeliPatterns.GetStartingCircle((250, 250), 150, 100);

for (int i = 0; i < 100; i++)
{
    await trafficControl.JoinNewGame($"{i}", "k");
}
while (trafficControl.CheckStatus().Result != TrafficControlService.GameStatus.Playing) ;

    for (int i = 0; i < trafficControl.Teams.Count; i++)
    {
        var team = trafficControl.Teams[i];
        var circlePos = helicircle[i];
        team.MoveHeliToPointAsync(circlePos);
    }
while (true)
{
}
