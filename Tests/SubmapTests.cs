using FluentAssertions;
using Roverlib.Models.Responses;
using Roverlib.Utils;

namespace Roverlib.Tests;

public class SubmapTests
{
    [Test]
    public void GetSubmap_ShouldReturnEmpty_WhenGivenEmptyMap()
    {
        var map = new Dictionary<long, Neighbor>();
        var submap = PathUtils.GetSubmap(map, (0, 0), (0, 0), 0);
        submap.Should().BeEmpty();
    }

    [Test]
    public void GetSubmap_ShouldReturnStraightLine_WhenNoBuffer()
    {
        List<Neighbor> Neighbors = CreateNeighbor(2, 1);
        Dictionary<long, Neighbor> map = CreateMap(Neighbors);

        var submap = PathUtils.GetSubmap(map, (0, 0), (1, 0), 0);
        AssertSubmap(
            submap,
            new[] { (0, 0), (1, 0) },
            new[] { Neighbors[0].Difficulty, Neighbors[1].Difficulty }
        );
    }

    [Test]
    public void GetSubmap_ShouldReturnStraightLine_WhenNoBuffer_ThreeNeighbors()
    {
        List<Neighbor> Neighbors = CreateNeighbor(3, 1);
        Dictionary<long, Neighbor> map = CreateMap(Neighbors);

        var submap = PathUtils.GetSubmap(map, (0, 0), (2, 0), 0);
        AssertSubmap(
            submap,
            new[] { (0, 0), (1, 0), (2, 0) },
            new[] { Neighbors[0].Difficulty, Neighbors[1].Difficulty, Neighbors[2].Difficulty }
        );
    }

    [Test]
    public void GetSubmap_ShouldReturnSquare_WhenBufferIsOne()
    {
        var Neighbors = CreateNeighbor(2, 2);
        var map = CreateMap(Neighbors);

        var submap = PathUtils.GetSubmap(map, (0, 0), (1, 1), 1);
        AssertSubmap(
            submap,
            new[] { (0, 0), (1, 0), (0, 1), (1, 1) },
            new[]
            {
                Neighbors[0].Difficulty,
                Neighbors[1].Difficulty,
                Neighbors[2].Difficulty,
                Neighbors[3].Difficulty
            }
        );
    }

    [Test]
    public void GetSubmap_ShouldReturnSquare_WhenBufferIsOne_ThreeNeighbors()
    {
        var Neighbors = CreateNeighbor(3, 3);
        var map = CreateMap(Neighbors);

        var submap = PathUtils.GetSubmap(map, (0, 0), (2, 2), 1);
        AssertSubmap(
            submap,
            new[] { (0, 0), (1, 0), (2, 0), (0, 1), (1, 1), (2, 1), (0, 2), (1, 2), (2, 2) },
            new[]
            {
                Neighbors[0].Difficulty,
                Neighbors[1].Difficulty,
                Neighbors[2].Difficulty,
                Neighbors[3].Difficulty,
                Neighbors[4].Difficulty,
                Neighbors[5].Difficulty,
                Neighbors[6].Difficulty,
                Neighbors[7].Difficulty,
                Neighbors[8].Difficulty
            }
        );
    }

    [Test]
    public void GetSubmap_WhenStartHigherThanTarget()
    {
        var Neighbors = CreateNeighbor(3, 3);
        var map = CreateMap(Neighbors);

        var submap = PathUtils.GetSubmap(map, (2, 2), (0, 0), 1);
        AssertSubmap(
            submap,
            new[] { (0, 0), (1, 0), (2, 0), (0, 1), (1, 1), (2, 1), (0, 2), (1, 2), (2, 2) },
            new[]
            {
                Neighbors[0].Difficulty,
                Neighbors[1].Difficulty,
                Neighbors[2].Difficulty,
                Neighbors[3].Difficulty,
                Neighbors[4].Difficulty,
                Neighbors[5].Difficulty,
                Neighbors[6].Difficulty,
                Neighbors[7].Difficulty,
                Neighbors[8].Difficulty
            }
        );
    }

    private void AssertSubmap(
        Dictionary<(int x, int y), int> submap,
        (int x, int y)[] expectedKeys,
        int[] expectedValues
    )
    {
        submap.Should().HaveCount(expectedKeys.Length);
        for (int i = 0; i < expectedKeys.Length; i++)
        {
            submap.Should().ContainKey(expectedKeys[i]);
            submap[expectedKeys[i]].Should().Be(expectedValues[i]);
        }
    }

    [Test]
    public void GetSubmap_ShouldReturnSquare_WhenBufferIsTwo()
    {
        var neighbors = CreateNeighbor(3, 3);
        var map = CreateMap(neighbors);

        var submap = PathUtils.GetSubmap(map, (0, 0), (1, 1), 2);

        submap.Should().HaveCount(9);

        AssertSubmap(
            submap,
            new[] { (0, 0), (1, 0), (2, 0), (0, 1), (1, 1), (2, 1), (0, 2), (1, 2), (2, 2) },
            new[]
            {
                neighbors[0].Difficulty,
                neighbors[1].Difficulty,
                neighbors[2].Difficulty,
                neighbors[3].Difficulty,
                neighbors[4].Difficulty,
                neighbors[5].Difficulty,
                neighbors[6].Difficulty,
                neighbors[7].Difficulty,
                neighbors[8].Difficulty
            }
        );
    }

    private static List<Neighbor> CreateNeighbor(int x, int y)
    {
        var Neighbors = new List<Neighbor>();
        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < y; j++)
            {
                Neighbors.Add(
                    new Neighbor
                    {
                        X = i,
                        Y = j,
                        Difficulty = 1
                    }
                );
            }
        }

        return Neighbors;
    }

    private static Dictionary<long, Neighbor> CreateMap(List<Neighbor> Neighbors)
    {
        return Neighbors.Select(n => (n.HashToLong(), n)).ToDictionary(x => x.Item1, x => x.n);
    }
}
