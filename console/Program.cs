using System;
using Roverlib.Models.Responses;
using Roverlib.Services;

HttpClient client = new HttpClient()
{
    BaseAddress = new Uri("https://snow-rover.azurewebsites.net/")
};

var gameManager = new GameManagerService(client);

for (int i = 0; i < 8; i++)
{
    await gameManager.JoinNewGame($"{i}", "g");
}

while (gameManager.CheckStatus().Result != GameManagerService.GameStatus.Playing) ;

while (true)
{
    foreach (var game in gameManager.Games)
    {
        try
        {
            game.MoveRoverAsync(Direction.Forward);
        }
        catch { }
    }
}
