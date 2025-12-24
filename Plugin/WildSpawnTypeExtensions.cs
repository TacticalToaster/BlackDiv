using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackDiv
{
    public static class WildSpawnTypeExtensions
    {
        public static List<int> TypeEnums = new List<int> { 848420, 848421, 848422, 848423, 848424 };

        public static bool IsBlackDiv(WildSpawnType type)
        {
            return TypeEnums.Contains((int)type);
        }
    }
}
