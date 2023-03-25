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
        submap.Should().HaveCount(2);
        submap.Should().ContainKey((0, 0));
        submap[(0, 0)].Should().Be(Neighbors[0].Difficulty);
        submap.Should().ContainKey((1, 0));
        submap[(1, 0)].Should().Be(Neighbors[1].Difficulty);
    }

    [Test]
    public void GetSubmap_ShouldReturnStraightLine_WhenNoBuffer_ThreeNeighbors()
    {
        List<Neighbor> Neighbors = CreateNeighbor(3, 1);
        Dictionary<long, Neighbor> map = CreateMap(Neighbors);

        var submap = PathUtils.GetSubmap(map, (0, 0), (2, 0), 0);
        submap.Should().HaveCount(3);
        submap.Should().ContainKey((0, 0));
        submap[(0, 0)].Should().Be(Neighbors[0].Difficulty);
        submap.Should().ContainKey((1, 0));
        submap[(1, 0)].Should().Be(Neighbors[1].Difficulty);
        submap.Should().ContainKey((2, 0));
        submap[(2, 0)].Should().Be(Neighbors[2].Difficulty);
    }

    // [Test]
    // public void GetSubmap_ShouldReturnSquare_WhenBufferIsOne()
    // {
    //     var Neighbors = CreateNeighbor(2, 2);
    //     var map = CreateMap(Neighbors);

    //     var submap = PathUtils.GetSubmap(map, (0, 0), (1, 1), 1);
    //     submap.Should().HaveCount(4);
    //     submap.Should().ContainKey((0, 0));
    //     submap[(0, 0)].Should().Be(Neighbors[0].Difficulty);
    //     submap.Should().ContainKey((1, 0));
    //     submap[(1, 0)].Should().Be(Neighbors[1].Difficulty);
    //     submap.Should().ContainKey((0, 1));
    //     submap[(0, 1)].Should().Be(Neighbors[2].Difficulty);
    //     submap.Should().ContainKey((1, 1));
    //     submap[(1, 1)].Should().Be(Neighbors[3].Difficulty);
    // }

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
