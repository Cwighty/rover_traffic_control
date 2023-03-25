using FluentAssertions;
using Roverlib.Models.Responses;
using Roverlib.Utils;

namespace Roverlib.Tests;

[TestFixture]
public class TravelingSalesmanTests
{
    [Test]
    public void GetDistance_ShouldReturnZero_WhenGivenEmptyRoute()
    {
        var route = new List<Location>();
        var distance = TravelingSalesman.GetDistance(route);
        distance.Should().Be(0);
    }

    [Test]
    public void GetDistance_ShouldReturnDistanceBetweenLocations_WhenGivenOneLocation()
    {
        var route = new List<Location> { new Location(0, 0), };
        var distance = TravelingSalesman.GetDistance(route);
        distance.Should().BeApproximately(0, 0.01);
    }

    [Test]
    public void GetDistance_ShouldReturnDistanceBetweenLocations_WhenGivenTwoLocations()
    {
        var route = new List<Location> { new Location(0, 0), new Location(0, 10) };
        var distance = TravelingSalesman.GetDistance(route);
        distance.Should().BeApproximately(10, 0.01);

        route = new List<Location> { new Location(0, 0), new Location(3, 4) };
        distance = TravelingSalesman.GetDistance(route);
        distance.Should().BeApproximately(5, 0.01);
    }

    [Test]
    public void GetShortestRoute_ShouldReturnEmpty_WhenGivenEmptyList()
    {
        var emptyList = new List<Location>();
        var shortestRoute = TravelingSalesman.GetShortestRoute(emptyList);
        shortestRoute.Should().BeEmpty();
    }

    [Test]
    public void GetShortestRoute_ShouldReturnSingleLocation_WhenGivenOneLocation()
    {
        var singleLocationList = new List<Location> { new Location(100, 200) };
        var shortestRoute = TravelingSalesman.GetShortestRoute(singleLocationList);
        shortestRoute.Should().HaveCount(1);
        shortestRoute[0].Should().Be(new Location(100, 200));
    }

    [Test]
    public void GetShortestRoute_ShouldReturnShortestRoute_WhenGivenThreeLocationsInOrder()
    {
        var locations = new List<Location>
        {
            new Location(0, 0),
            new Location(2, 2),
            new Location(5, 5),
        };
        var shortestRoute = TravelingSalesman.GetShortestRoute(locations);
        shortestRoute.Should().HaveCount(3);
        shortestRoute[0].Should().Be(new Location(0, 0));
        shortestRoute[1].Should().Be(new Location(2, 2));
        shortestRoute[2].Should().Be(new Location(5, 5));
    }

    [Test]
    public void GetShortestRoute_ShouldReturnShortestRoute_WhenGivenThreeLocationsOutOfOrder()
    {
        var locations = new List<Location>
        {
            new Location(2, 2),
            new Location(0, 0),
            new Location(5, 5),
        };
        var shortestRoute = TravelingSalesman.GetShortestRoute(locations);
        shortestRoute.Should().HaveCount(3);
        shortestRoute[0].Should().Be(new Location(0, 0));
        shortestRoute[1].Should().Be(new Location(2, 2));
        shortestRoute[2].Should().Be(new Location(5, 5));
    }

    [Test]
    public void GetShortestRoute_ShouldReturnShortestRoute_WhenGivenFourLocationsOutOfOrder_ThreeClumps()
    {
        var locations = new List<Location>
        {
            new Location(0, 1),
            new Location(0, 0),
            new Location(10, 10),
            new Location(20, 1),
        };
        var shortestRoute = TravelingSalesman.GetShortestRoute(locations);
        shortestRoute.Should().HaveCount(4);
        shortestRoute[0].Should().Be(new Location(0, 0));
        shortestRoute[1].Should().Be(new Location(0, 1));
        shortestRoute[2].Should().Be(new Location(10, 10));
        shortestRoute[3].Should().Be(new Location(20, 1));
    }
}
