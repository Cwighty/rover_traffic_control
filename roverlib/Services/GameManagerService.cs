﻿using System.Net.Http.Json;
using Roverlib.Models;
using Roverlib.Models.Responses;

namespace Roverlib.Services;
public partial class GameManagerService
{
    private readonly HttpClient client;
    public List<Game> Games { get; set; }
    public GameManagerService(HttpClient client)
    {
        this.client = client;
        Games = new();
    }

    public async Task JoinNewGame(string name, string gameid)
    {
        var result = await client.GetAsync($"/Game/Join?gameId={gameid}&name={name}");
        if (result.IsSuccessStatusCode)
        {
            var res = await result.Content.ReadFromJsonAsync<JoinResponse>();
            Games.Add(new Game(name, gameid, res, client));
        }
        else
        {
            var res = await result.Content.ReadFromJsonAsync<ProblemDetail>();
            throw new ProblemDetailException(res);
        }
    }

    public enum GameStatus
    {
        Playing,
        Joining,
        Invalid
    }

    public async Task<GameStatus> CheckStatus()
    {
        if (Games.Count == 0) return GameStatus.Invalid;
        var res = await client.GetAsync($"/Game/Status?token={Games[0].Token}");
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


    // public async Task MovePerserveranceToPointAsync(int x, int y)
    // {
    //     _ = CurrentGame.PerserveranceRover.Location;
    //     var targetLoc = new Location(x, y);
    //     try
    //     {
    //         Location curLoc = CurrentGame.PerserveranceRover.Location;
    //         var curOrientation = CurrentGame.PerserveranceRover.Orientation;
    //         while (curOrientation != Orientation.North)
    //         {
    //             await MovePerserveranceAsync(Direction.Right);
    //             curOrientation = CurrentGame.PerserveranceRover.Orientation;
    //         }
    //         while (curLoc.Column < targetLoc.Column)
    //         {
    //             await MovePerserveranceAsync(Direction.Forward);
    //             curLoc = CurrentGame.PerserveranceRover.Location;
    //         }
    //         while (curLoc.Column > targetLoc.Column)
    //         {
    //             await MovePerserveranceAsync(Direction.Reverse);
    //             curLoc = CurrentGame.PerserveranceRover.Location;
    //         }

    //         while (curOrientation != Orientation.East)
    //         {
    //             await MovePerserveranceAsync(Direction.Right);
    //             curOrientation = CurrentGame.PerserveranceRover.Orientation;
    //         }
    //         while (curLoc.Row < targetLoc.Row)
    //         {
    //             await MovePerserveranceAsync(Direction.Forward);
    //             curLoc = CurrentGame.PerserveranceRover.Location;
    //         }
    //         while (curLoc.Row > targetLoc.Row)
    //         {
    //             await MovePerserveranceAsync(Direction.Reverse);
    //             curLoc = CurrentGame.PerserveranceRover.Location;
    //         }
    //     }
    //     catch { }
    // }
}


