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
        var result = await client.GetAsync($"/Game/Join?gameId={gameid}&name={name}");
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
}


