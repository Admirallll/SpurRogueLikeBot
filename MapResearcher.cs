using SpurRoguelike.Core.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpurRoguelike.PlayerBot
{
    public static class MapResearcher
    {
        public static ResearchResult FindShortestPaths(Map map, Location start)
        {
            var track = new Dictionary<Location?, Location?>();
            var tree = new BinaryTree();
            tree.Add(0, start);
            var distances = new Dictionary<Location, int>();
            var delts = new Offset[] { new Offset(-1, 0), new Offset(0, -1), new Offset(1, 0), new Offset(0, 1) };
            track[start] = null;
            var visited = new HashSet<Location>();
            while (tree.Root != null)
            {
                TreeNode minNode = tree.ExtractMin();
                visited.Add(minNode.Value);
                var toOpen = minNode.Value;
                var currentPrice = minNode.Key;
                distances[toOpen] = currentPrice;
                if (!map.IsEmptyPoint(toOpen.X, toOpen.Y))
                    continue;
                foreach (var delt in delts)
                {
                    Location currentLoc = toOpen + delt;
                    if (map.IsPointOnMap(currentLoc.X, currentLoc.Y)
                        && !visited.Contains(currentLoc)
                        && map.Points[currentLoc.X, currentLoc.Y].Type != MapPointType.NonPassable)
                    {
                        var newPrice = currentPrice + map.Points[currentLoc.X, currentLoc.Y].Weight;
                        if (!tree.valueToKey.ContainsKey(currentLoc))
                        {
                            tree.Add(newPrice, currentLoc);
                            track[currentLoc] = toOpen;
                        }
                        else if (tree.valueToKey[currentLoc] > newPrice)
                        {
                            tree.Update(currentLoc, newPrice);
                            track[currentLoc] = toOpen;
                        }
                        tree.Update(currentLoc, newPrice);
                    }
                }
            }
            return new ResearchResult(track, distances);
        }

        public static int GetNeighboursCount(Map map, Location loc)
        {
            var count = 0;
            foreach (var neighbour in GetNeighboursLocations(loc))
                if (!map.IsPointOnMap(neighbour.X, neighbour.Y) || !map.IsEmptyPoint(neighbour.X, neighbour.Y))
                    count++;
            return count;
        }

        public static IEnumerable<Location> GetNeighboursLocations(Location loc)
        {
            var delts = new Offset[] { new Offset(-1, 0), new Offset(0, -1), new Offset(1, 0), new Offset(0, 1) };
            foreach (var delt in delts)
                yield return loc + delt;
        }

        public static Location GetMinDensityLocation(IEnumerable<Location> cells, Map map)
        {
            Location minDensityLocation = cells.FirstOrDefault();
            foreach (var cell in cells)
            {
                if (map.GetCellDensities(minDensityLocation, 3)[3] > map.GetCellDensities(cell, 3)[3])
                    minDensityLocation = cell;
            }
            return minDensityLocation;
        }


    }


    public class ResearchResult
    {
        public readonly Dictionary<Location?, Location?> Track;
        public readonly Dictionary<Location, int> Distances;

        public ResearchResult(Dictionary<Location?, Location?> track, Dictionary<Location, int> distances)
        {
            Track = track;
            Distances = distances;
        }
    }
}
