using BepInEx;
using System.Collections.Generic;

namespace BlackDiv.Prepatch
{
    [BepInDependency("com.morebotsapiprepatch.tacticaltoaster", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(ClientInfo.PreLoadGUID, ClientInfo.PreLoadName, ClientInfo.Version)]
    public class MoreBotsPrepatchExample : BaseUnityPlugin
    {
        public static MoreBotsPrepatchExample Instance;

        public void Awake()
        {
            Instance = this;
        }
    }
}
