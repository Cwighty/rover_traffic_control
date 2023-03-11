using System.Collections.Concurrent;
using System.Text;
using Roverlib.Models.Responses;

public static class MapHelper
{
    public static ConcurrentDictionary<long, Neighbor> InitializeMap(List<LowResolutionMap> lowResolutionMap)
    {
        // get the file path
        var path = $"/maps/{GetFileNameFromMap(lowResolutionMap)}";
        // check if the file exists
        if (File.Exists(path))
        {
            // read the map from the file
            return ReadMapFromCSV(path);
        }
        else
        {
            // create the map from the low resolution map
            return InitializeDefaultMap(lowResolutionMap);
        }
    }

    public static ConcurrentDictionary<long, Neighbor> InitializeDefaultMap(List<LowResolutionMap> lowResolutionMap)
    {
        // create the map from the low resolution map
        var map = new ConcurrentDictionary<long, Neighbor>();
        foreach (var lowResMap in lowResolutionMap)
        {
            for (int x = lowResMap.lowerLeftX; x <= lowResMap.upperRightX; x++)
            {
                for (int y = lowResMap.lowerLeftY; y <= lowResMap.upperRightY; y++)
                {
                    var n = new Neighbor()
                    {
                        X = x,
                        Y = y,
                        Difficulty = lowResMap.averageDifficulty
                    };
                    map.TryAdd(n.HashToLong(), n);
                };
            }
        }
        return map;
    }
    public static ConcurrentDictionary<long, Neighbor> ReadMapFromCSV(string path)
    {
        var map = new ConcurrentDictionary<long, Neighbor>();
        var lines = File.ReadAllLines(path);
        for (int y = 0; y < lines.Length; y++)
        {
            var line = lines[y];
            var cells = line.Split(',');
            for (int x = 0; x < cells.Length; x++)
            {
                var cell = cells[x];
                var n = new Neighbor()
                {
                    X = x,
                    Y = y,
                    Difficulty = int.Parse(cell)
                };
                map.TryAdd(n.HashToLong(), n);
            }
        }
        return map;
    }


    public static void WriteMapToCSV(ConcurrentDictionary<long, Neighbor> map, string path)
    {
        var lines = new List<string>();
        var maxX = map.Max(x => x.Value.X);
        var maxY = map.Max(x => x.Value.Y);
        for (int y = 0; y <= maxY; y++)
        {
            var line = new List<string>();
            for (int x = 0; x <= maxX; x++)
            {
                var cell = map[new Neighbor() { X = x, Y = y }.HashToLong()].Difficulty;
                line.Add(cell.ToString());
            }
            lines.Add(string.Join(',', line));
        }
        File.WriteAllLines(path, lines);
    }

    public static string HashLowResMap(List<LowResolutionMap> lowResMap)
    {
        var sb = new StringBuilder();
        foreach (var map in lowResMap)
        {
            sb.Append(map.lowerLeftX);
            sb.Append(map.lowerLeftY);
            sb.Append(map.upperRightX);
            sb.Append(map.upperRightY);
            sb.Append(map.averageDifficulty);
        }
        var s = sb.ToString();
        var hashcode = String.Format("{0:X}", s.GetHashCode());
        return hashcode;
    }

    public static string GetFileNameFromMap(List<LowResolutionMap> lowResMap)
    {
        var hashcode = HashLowResMap(lowResMap);
        var filename = $"map_{hashcode}.csv";
        return filename;
    }

}