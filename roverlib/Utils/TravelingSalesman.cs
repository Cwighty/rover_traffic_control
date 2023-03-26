using Roverlib.Models.Responses;

namespace Roverlib.Utils;

public static class TravelingSalesman
{
    public static List<Location> GetShortestRoute(List<Location> locations)
    {
        var permutations = GetPermutations(locations);
        var shortestDistance = double.MaxValue;
        List<Location> shortestRoute = null;

        foreach (var route in permutations)
        {
            var distance = GetDistance(route);

            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                shortestRoute = route.ToList();
            }
        }

        return new List<Location>(shortestRoute);
    }

    public static List<List<Location>> GetPermutations(List<Location> list)
    {
        var permutations = new List<List<Location>>();

        if (list.Count == 0)
        {
            permutations.Add(new List<Location>());
            return permutations;
        }

        var startingElementIndex = 0;

        foreach (var element in list)
        {
            var remainingList = new List<Location>();

            for (var i = 0; i < list.Count; i++)
            {
                if (i != startingElementIndex)
                    remainingList.Add(list[i]);
            }

            var subPermutations = GetPermutations(remainingList);

            foreach (var permutation in subPermutations)
            {
                permutation.Insert(0, element);
                permutations.Add(permutation);
            }

            startingElementIndex++;
        }

        return permutations;
    }

    public static double GetDistance(List<Location> route)
    {
        var totalDistance = 0.0;

        for (var i = 0; i < route.Count - 1; i++)
        {
            var fromLocation = route[i];
            var toLocation = route[i + 1];
            var distance = PathUtils.EuclideanDistance(fromLocation, toLocation);
            totalDistance += distance;
        }

        return totalDistance;
    }

    public static List<Location> GetClosestEdgePoints(
        List<Location> locations,
        int mapWidth,
        int mapHeight
    )
    {
        var closestEdgePoints = new List<Location>();

        foreach (var target in locations)
        {
            double closestDistance = double.MaxValue;
            Location closestEdgePoint = null;

            // Check top and bottom edges
            for (int x = 0; x < mapWidth; x++)
            {
                double distanceToEdge = PathUtils.EuclideanDistance(target, new Location(x, 0));
                if (distanceToEdge < closestDistance)
                {
                    closestDistance = distanceToEdge;
                    closestEdgePoint = new Location(x, 0);
                }

                distanceToEdge = PathUtils.EuclideanDistance(
                    target,
                    new Location(x, mapHeight - 1)
                );
                if (distanceToEdge < closestDistance)
                {
                    closestDistance = distanceToEdge;
                    closestEdgePoint = new Location(x, mapHeight - 1);
                }
            }

            // Check left and right edges
            for (int y = 0; y < mapHeight; y++)
            {
                double distanceToEdge = PathUtils.EuclideanDistance(target, new Location(0, y));
                if (distanceToEdge < closestDistance)
                {
                    closestDistance = distanceToEdge;
                    closestEdgePoint = new Location(0, y);
                }

                distanceToEdge = PathUtils.EuclideanDistance(target, new Location(mapWidth - 1, y));
                if (distanceToEdge < closestDistance)
                {
                    closestDistance = distanceToEdge;
                    closestEdgePoint = new Location(mapWidth - 1, y);
                }
            }

            closestEdgePoints.Add(closestEdgePoint);
        }

        return closestEdgePoints;
    }

    public static List<Location> GetBestRoute(List<Location> locations, int mapWidth, int mapHeight)
    {
        var closestEdgePoints = GetClosestEdgePoints(locations, mapWidth, mapHeight);
        var bestRoute = new List<Location>();
        var bestRouteDistance = double.MaxValue;
        var permutations = GetPermutations(locations);
        foreach (var edgePoint in closestEdgePoints)
        {
            foreach (var permutation in permutations)
            {
                var route = new List<Location>();
                route.Add(edgePoint);
                route.AddRange(permutation);
                var distance = GetDistance(route);
                if (distance < bestRouteDistance)
                {
                    bestRouteDistance = distance;
                    bestRoute = route;
                }
            }
        }
        return bestRoute;
    }
}
