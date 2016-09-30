using System.Linq;
using SpurRoguelike.Core;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;
using System.Collections.Generic;

namespace SpurRoguelike.PlayerBot
{
    public class PlayerBot : IPlayerController
    {
        public static ObjectDangerous MonsterDangerous = new ObjectDangerous { Value = 15, Range = 2 };
        public static ObjectDangerous TrapDangerous = new ObjectDangerous { Value = 1000000000, Range = 1 };
        public static ObjectDangerous ExitDangerous = new ObjectDangerous { Value = 100000000, Range = 1 };
        public static ObjectDangerous WallDangerous = new ObjectDangerous { Value = 1, Range = 3 };
        public static ObjectDangerous HealthDangerous = new ObjectDangerous { Value = 100000, Range = 1 };
        public static ObjectDangerous ItemDangerous = new ObjectDangerous { Value = 1000000, Range = 1 };

        public Stack<Location> Path { get; set; }
        private Dictionary<Location?, Location?> track;
        private Dictionary<Location, int> distances;
        private LevelView level;
        private Map map;

        public Turn MakeTurn(LevelView levelView, IMessageReporter messageReporter)
        {
            level = levelView;
            if (levelView.Field.GetCellsOfType(CellType.PlayerStart).FirstOrDefault() == level.Player.Location)
            {
                map = null;
            }
            if (map == null)
            {
                map = new Map(level);
                map.CreateStaticObjectsCopy();
                RefreshMapAndTrack();
            }
            if (level.Monsters.Count() == 0 && IsBossLevel())
                map.CreateStaticObjectsCopy();
            RefreshMapAndTrack();
            var playerLoc = level.Player.Location;
            var anyEnemyInAttackRange = level.Monsters.FirstOrDefault(m => IsInAttackRange(m.Location, playerLoc));
            var anyHealthInRange = level.HealthPacks.FirstOrDefault(x => x.Location.IsInRange(playerLoc, 1));
            if (IsBossLevel())
                return BossFighting();
            if (NeedHealthPack(levelView, anyEnemyInAttackRange.HasValue))
            {
                var packs = levelView.HealthPacks.Select(x => x.Location);
                Location? nearestPack = null;
                for (int i = 0; i < 4; i++)
                {
                    nearestPack = GetNearestCell(packs.Where(x => MapResearcher.GetNeighboursCount(map, x) <= i), distances);
                    if (nearestPack != null)
                        break;
                }
                Path = GetPathFromTrack(track, nearestPack, playerLoc);
                if (Path == null)
                    return Turn.Attack(anyEnemyInAttackRange.Location - levelView.Player.Location);

            }
            else if (anyEnemyInAttackRange.HasValue && !NeedHealthPack(levelView, anyEnemyInAttackRange.HasValue))
                return Turn.Attack(anyEnemyInAttackRange.Location - levelView.Player.Location);
            else 
            {
                var nearestMonster = GetNearestCell(levelView.Monsters.Select(x => x.Location), distances);
                var nearestHealth = GetNearestCell(levelView.HealthPacks.Select(x => x.Location), distances);
                if (CountOfCells(level.Monsters.Select(x => x.Location)) > 0 && CountOfCells(level.HealthPacks.Select(x => x.Location)) > 0)
                {
                    Path = GetPathFromTrack(track, nearestMonster, playerLoc);
                }
                else if (levelView.Player.Health < 100 && nearestHealth.HasValue)
                {
                    Path = GetPathFromTrack(track, nearestHealth, playerLoc);
                }
                else
                {
                    ItemView bestEquip;
                    if (TryFindBestEquipment(levelView, out bestEquip))
                        Path = GetPathFromTrack(track, bestEquip.Location, playerLoc);
                    else
                        Path = GetPathFromTrack(track, levelView.Field.GetCellsOfType(CellType.Exit).FirstOrDefault(), playerLoc);
                }
                
            }
            Location? nextLocation = Path.Pop();
            return Turn.Step((Location)nextLocation - levelView.Player.Location);
        }

        private void RefreshMapAndTrack()
        {
            map.FillMap();
            var researchResult = MapResearcher.FindShortestPaths(map, level.Player.Location);
            track = researchResult.Track;
            distances = researchResult.Distances;
        }

        private bool startFinalFight;
        private Location? runAwayLocation;
        private bool startBossFight;
        private int prevPlayerHealth;
        private int prevBossHealth;

        private Turn BossFighting()
        {
            var boss = level.Monsters.First();
            var player = level.Player;
            if (prevPlayerHealth != 100 && prevPlayerHealth > player.Health || prevBossHealth > 0 && prevBossHealth < boss.Health)
            {
                startBossFight = true;
            }
            prevPlayerHealth = player.Health;
            prevBossHealth = boss.Health;
            if (!startBossFight)
            {
                if (player.Health < 50)
                {
                    var nearestPack = GetNearestCell(level.HealthPacks.Select(x => x.Location), distances);
                    Path = GetPathFromTrack(track, nearestPack, player.Location);
                    return Turn.Step(Path.First() - player.Location);
                }
                else if (boss.Location.IsInRange(player.Location, 1))
                {
                    Path = null;
                    return Turn.Attack(boss.Location - player.Location);
                }
                else
                {
                    if (Path == null || Path.Count == 0)
                        Path = GetPathFromTrack(track, boss.Location, player.Location);

                    return Turn.Step(Path.Pop() - player.Location);
                }
            }
            if (player.Health > 50)
            {
                ItemView bestEquip;
                if (TryFindBestEquipment(level, out bestEquip))
                    return Turn.Step(GetPathFromTrack(track, bestEquip.Location, level.Player.Location).First() - player.Location);
            }
            var bossLosingHp = (int)((double)player.TotalAttack * 10 / boss.TotalDefence) - 1;
            var playerLosingHp = (int)((double)boss.TotalAttack * 10 / player.TotalDefence);
            var playerHealth = 200 - 3 * playerLosingHp;
            var bossDamagedHealths = (playerHealth / playerLosingHp) * bossLosingHp;
            if (bossDamagedHealths < boss.Health && player.Health > 50)
            {
                var pathToBoss = GetPathFromTrack(track, boss.Location, player.Location);
                if (!player.Location.IsInRange(boss.Location, 1))
                    return Turn.Step(pathToBoss.First() - player.Location);
                else
                    return Turn.Attack(boss.Location - player.Location);
            }
            
            if (level.HealthPacks.Count() > 2)
            {
                Location? nearestPack = null;
                for (int i = 3; i >= 0; i--)
                {
                    nearestPack = GetNearestCell(level.HealthPacks.Select(x => x.Location).Where(x => MapResearcher.GetNeighboursCount(map, x) >= i), distances);
                    if (nearestPack != null)
                        break;
                }
                return Turn.Step(GetPathFromTrack(track, nearestPack, player.Location).First() - player.Location);
            }

            if (level.HealthPacks.Count() == 2)
            {
                var range = 5;
                var value = 1000;
                foreach (var health in level.HealthPacks)
                    map.AddWeightAround(health.Location, range, value, null, (x, y, z, t) => t);
                var firstHealth = level.HealthPacks.First().Location;
                var secondHealth = level.HealthPacks.Last().Location;
                if (!firstHealth.IsInRange(player.Location, range) && !secondHealth.IsInRange(player.Location, range))
                    startFinalFight = true;

                if (!startFinalFight)
                {
                    if (runAwayLocation == null)
                    {
                        for (int x = 0; x < map.Points.GetLength(0); x++)
                            for (int y = 0; y < map.Points.GetLength(1); y++)
                                if (map.IsPointOnMap(x, y) && map.IsEmptyPoint(x, y))
                                {
                                    var loc = new Location(x, y);
                                    if (!loc.IsInRange(firstHealth, range) && !loc.IsInRange(secondHealth, range))
                                        runAwayLocation = loc;
                                    break;
                                }
                    }
                    var path = GetPathFromTrack(track, runAwayLocation, player.Location);
                    return Turn.Step(path.First() - player.Location);                                                                                                            
                }

                var xDirection = firstHealth.X - secondHealth.X;
                var yDirection = firstHealth.Y - secondHealth.Y;
                if (xDirection < 0)
                    xDirection = -1;
                if (xDirection > 0)
                    xDirection = 1;
                if (yDirection < 0)
                    yDirection = -1;
                if (yDirection > 0)
                    yDirection = 1;

                for (int i = 1; i <= range; i++)
                {
                    if (map.IsPointOnMap(firstHealth.X + i * xDirection, firstHealth.Y))
                        map.Points[firstHealth.X + i * xDirection, firstHealth.Y].Weight -= value;
                    if (map.IsPointOnMap(firstHealth.X, firstHealth.Y + i * yDirection))
                        map.Points[firstHealth.X, firstHealth.Y + i * yDirection].Weight -= value;
                    if (map.IsPointOnMap(secondHealth.X + i * xDirection * -1, secondHealth.Y))
                        map.Points[secondHealth.X + i * xDirection * -1, secondHealth.Y].Weight -= value;
                    if (map.IsPointOnMap(secondHealth.X, secondHealth.Y + i * yDirection * -1))
                        map.Points[secondHealth.X, secondHealth.Y + i * yDirection * -1].Weight -= value;
                }
                var researchResult = MapResearcher.FindShortestPaths(map, player.Location);
                track = researchResult.Track;
                distances = researchResult.Distances;
            }
            if (level.HealthPacks.Count() == 1 || level.HealthPacks.Count() == 2)
            {
                var nearestPack = GetNearestCell(level.HealthPacks.Select(x => x.Location), distances);
                var healthPath = GetPathFromTrack(track, nearestPack, player.Location);
                if (healthPath.First() != nearestPack)
                    return Turn.Step(healthPath.First() - player.Location);
                else
                {
                    if (nearestPack.Value.IsInRange(boss.Location, 1) || player.Health <= playerLosingHp + 1)
                        return Turn.Step(healthPath.First() - player.Location);
                    else if (player.Location.IsInRange(boss.Location, 1))
                        return Turn.Attack(boss.Location - player.Location);
                    else
                        return Turn.None;
                }
            }
            var bossPath = GetPathFromTrack(track, boss.Location, player.Location);
            if (!player.Location.IsInRange(boss.Location, 1))
                return Turn.Step(bossPath.First() - player.Location);
            else
                return Turn.Attack(boss.Location - player.Location);
        }

        public int CountOfCells(IEnumerable<Location> cells)
        {
            int count = 0;
            foreach (var cell in cells)
                if (distances.ContainsKey(cell))
                    count++;
            return count;
        }

        private bool IsBossLevel()
        {
            return MapResearcher.GetNeighboursCount(map, level.Field.GetCellsOfType(CellType.Exit).First()) == 4;
        }

        private bool CanFighting(LevelView level, int range)
        {
            var damage = level.Monsters.Where(x => x.Location.IsInRange(level.Player.Location, range)).Sum(x => x.TotalAttack) / level.Player.TotalDefence;
            var health = CellsInRange(level.Player.Location, level.HealthPacks.Select(x => x.Location), range) * 50;
            return health >= damage;
        }

        private int CellsInRange(Location loc, IEnumerable<Location> cells, int range)
        {
            return cells.Where(x => x.IsInRange(loc, range)).Count();
        }

        private Location HealthfulLocation(IEnumerable<Location> locations, Dictionary<Location, int> distances, int range)
        {
            Location? maxLoc = null;
            var maxCount = 0;
            foreach (var loc in locations)
            {
                if (!distances.ContainsKey(loc))
                    continue;
                var healthsNear = 0;
                foreach (var nearLoc in locations)
                    if (loc != nearLoc && loc.IsInRange(nearLoc, range))
                        healthsNear++;
                if (maxLoc == null || maxCount < healthsNear)
                {
                    maxCount = healthsNear;
                    maxLoc = loc;
                }
            }
            return (Location)maxLoc;
        }

        private bool IsInAttackRange(Location a, Location b)
        {
            return a.IsInRange(b, 1);
        }

        public int FindRadius(Location target, LevelView levelView)
        {
            return (target - levelView.Player.Location).Size();
        }

        public Location? GetNearestCell(IEnumerable<Location> cells, Dictionary<Location, int> distances)
        {
            Location? nearest = null;
            foreach (var cell in cells)
            {
                if (distances.ContainsKey(cell) && (nearest == null || (distances.ContainsKey((Location)nearest) && distances[cell] < distances[(Location)nearest])))
                    nearest = cell;
            }
            return nearest;
        }

        public Stack<Location> GetPathFromTrack(Dictionary<Location?, Location?> track, Location? target, Location start)
        {
            var path = new Stack<Location>();
            var currentLoc = target;
            if (target == null || !track.ContainsKey(target))
                return null;
            while (currentLoc != start)
            {
                path.Push((Location)currentLoc);
                currentLoc = (Location)track[currentLoc];
            }
            return path;
        }

        public bool NeedHealthPack(LevelView levelView, bool IsEnemyInAttackRange)
        {
            return levelView.HealthPacks.Count() > 0 && (levelView.Monsters.Where(x => x.Location.IsInRange(levelView.Player.Location, 3)).Count() > 3
                || levelView.Player.Health < 60
                || levelView.Monsters.Where(m => IsInAttackRange(m.Location, levelView.Player.Location)).Count() > 1);
        }

        public bool TryFindBestEquipment(LevelView levelView, out ItemView bestEquip)
        {
            ItemView playerItem;
            if (levelView.Player.TryGetEquippedItem(out playerItem))
                bestEquip = playerItem;
            else
                bestEquip = new ItemView(new Core.Entities.Item("DummyItem", 0, 0));
            foreach (var item in levelView.Items)
                if (bestEquip.HasValue && item.AttackBonus + item.DefenceBonus > bestEquip.AttackBonus + bestEquip.DefenceBonus)
                    bestEquip = item;
            return playerItem.Location != bestEquip.Location;
        }
    }
}
