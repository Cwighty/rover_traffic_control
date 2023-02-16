using Roverlib.Services;

HttpClient client = new HttpClient()
{
  BaseAddress = new Uri("https://snow-rover.azurewebsites.net/")
};

var trafficControl = new TrafficControlService(client);

var helicircle = HeliPatterns.GetStartingCircle((250, 250), 150, 10);

for (int i = 0; i < 10; i++)
{
  await trafficControl.JoinNewGame($"{i}", "y");
}
while (trafficControl.CheckStatus().Result != TrafficControlService.GameStatus.Playing) ;
List<Task> moveHelis = new List<Task>();
for (int i = 0; i < trafficControl.Teams.Count; i++)
{
  var team = trafficControl.Teams[i];
  var circlePos = helicircle[i];
  var task = team.MoveHeliToPointAsync(circlePos);
  moveHelis.Add(task);
}
try
{
  Task.WaitAll(moveHelis.ToArray());
}
catch { }
var rotation = HeliPatterns.RotateList(helicircle, 1);
for (int i = 0; i < 100; i++)
{
  rotation = HeliPatterns.RotateList(rotation, 1);
  var rotateHelis = new List<Task>();
  for (int j = 0; j < trafficControl.Teams.Count; j++)
  {
    var team = trafficControl.Teams[j];
    var circlePos = rotation[j];
    var task = team.MoveHeliToPointAsync(circlePos);
    rotateHelis.Add(task);
  }
  try
  {
    Task.WaitAll(rotateHelis.ToArray());
  }
  catch { }
}

while (true)
{
}
