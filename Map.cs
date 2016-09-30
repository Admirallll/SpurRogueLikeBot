using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;
using System.IO;

namespace SpurRoguelike.PlayerBot
{
    public class Map
    {
        public MapPoint[,] Points;
        private readonly LevelView level;
        private MapPoint[,] StaticPoints;

        public Map(LevelView level)
        {
            this.level = level;
        }

        public void FillMap()
        {
            Points = CreateCopyFromStatic();
            PlaceObjects(Points, level.Monsters.Select(x => x.Location), MapPointType.Passable, PlayerBot.MonsterDangerous);
            PlaceObjects(Points, level.Items.Select(x => x.Location), MapPointType.Empty, PlayerBot.ItemDangerous);
            PlaceObjects(Points, level.HealthPacks.Select(x => x.Location), MapPointType.Empty, PlayerBot.HealthDangerous);
            PlaceObjects(Points, level.Field.GetCellsOfType(CellType.Trap), MapPointType.NonPassable, PlayerBot.TrapDangerous);
        }

        public MapPoint[,] CreateCopyFromStatic()
        {
            var points = new MapPoint[StaticPoints.GetLength(0), StaticPoints.GetLength(1)];
            for (int x = 0; x < StaticPoints.GetLength(0); x++)
                for (int y = 0; y < StaticPoints.GetLength(1); y++)
                    points[x, y] = new MapPoint(StaticPoints[x, y].Type, StaticPoints[x, y].Weight);
            return points;
        }

        public void CreateStaticObjectsCopy()
        {
            StaticPoints = new MapPoint[level.Field.Width, level.Field.Height];
            for (int x = 0; x < StaticPoints.GetLength(0); x++)
                for (int y = 0; y < StaticPoints.GetLength(1); y++)
                    StaticPoints[x, y] = new MapPoint(MapPointType.Empty);
            
            PlaceObjects(StaticPoints, level.Field.GetCellsOfType(CellType.Wall), MapPointType.NonPassable, PlayerBot.WallDangerous);
            PlaceObjects(StaticPoints, level.Field.GetCellsOfType(CellType.Exit), MapPointType.Passable, PlayerBot.ExitDangerous);
        }

        public void PlaceObjects(MapPoint[,] points, IEnumerable<Location> objects, MapPointType type, ObjectDangerous dangerous)
        {
            foreach (var obj in objects)
            {
                points[obj.X, obj.Y].SetType(type);
                if (dangerous == null) continue;
                AddWeightAround(obj, dangerous.Range, dangerous.Value, points, CalculateDecreasingWeight);
            }
        }

        public bool IsPointOnMap(int x, int y)
        {
            return x >= 0 && x < level.Field.Width && y >= 0 && y < level.Field.Height;
        }

        public bool IsEmptyPoint(int x, int y)
        {
            return IsPointOnMap(x, y) && Points[x, y].Type == MapPointType.Empty;
        }

        public List<double> GetCellDensities(Location cellLocation, int maxRadius)
        {
            var loc = cellLocation;
            var cellDensities = new List<double>();
            var density = 0;
            for (int radius = 0; radius <= maxRadius; radius++)
            {
                for (int x = -radius; x <= radius; x++)
                    for (int y = -radius; y <= radius; y++)
                        if (x == -radius || y == -radius || x == maxRadius || y == maxRadius)
                        {
                            var currentX = x + loc.X;
                            var currentY = y + loc.Y;
                            if (IsPointOnMap(currentX, currentY) && !IsEmptyPoint(currentX, currentY))
                                density++;
                        }
                cellDensities.Add(density);
            }
            return cellDensities;
        }

        public void AddWeightAround(Location loc, int range, int value, MapPoint[,] points, Func<int, int, int, int, int> weightCalc)
        {
            if (points == null)
                points = Points;
            for (int x = -range; x <= range; x++)
                for (int y = -range; y <= range; y++)
                {
                    var currentX = loc.X + x;
                    var currentY = loc.Y + y;
                    if (IsPointOnMap(currentX, currentY))
                        points[currentX, currentY].Weight += weightCalc(x, y, range, value);
                }
        }

        private int CalculateDecreasingWeight(int x, int y, int range, int value)
        {
            return value - Math.Max(Math.Abs(x), Math.Abs(y)) * (value / range);
        }

        public void WriteMapToFile(string filename)
        {
            var content = new List<string>();
            for (int y = 0; y < Points.GetLength(1); y++)
            {
                content.Add("");
                for (int x = 0; x < Points.GetLength(0); x++)
                    content[y] += Points[x, y].Weight > 9 ? 9 : Points[x, y].Weight;
            }
            File.WriteAllLines(filename, content);
        }
    }
}
