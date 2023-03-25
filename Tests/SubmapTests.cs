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

 
}
