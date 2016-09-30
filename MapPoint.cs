using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpurRoguelike.PlayerBot
{
    public class MapPoint
    {
        public MapPointType Type { get; private set; }
        public int Weight { get; set; }

        public MapPoint(MapPointType type, int weight = 1)
        {
            Type = type;
            Weight = weight;
        }

        public void SetType(MapPointType newType)
        { 
            Type = newType;
        }
    }
}
