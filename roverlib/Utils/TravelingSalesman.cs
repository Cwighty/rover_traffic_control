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

        return shortestRoute;
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
}
