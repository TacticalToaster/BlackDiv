using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BlackDiv.Patches;
using System;
using System.Collections.Generic;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using MoreBotsAPI.Behavior.Layers;
using MoreBotsAPI.Components;

namespace BlackDiv
{
    [BepInDependency("xyz.drakia.bigbrain")]
    [BepInDependency("me.sol.sain", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.morebotsapi.tacticaltoaster")]
    [BepInPlugin(ClientInfo.GUID, ClientInfo.PluginName, ClientInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;

        // BaseUnityPlugin inherits MonoBehaviour, so you can use base unity functions like Awake() and Update()
        private void Awake()
        {
            // save the Logger to variable so we can use it elsewhere in the project
            LogSource = Logger;

            new TarkovInitPatch().Enable();
            //new BotOwnerActivatePatch().Enable();
            //new BotsControllerInitPatch().Enable();
            new BDNvgPatch().Enable();

            var bdEnums = new List<int> { 848420, 848421, 848422, 848423, 848424, 848426 }
                .ConvertAll(x => (WildSpawnType)x);
            
            MonoBehaviourSingleton<HuntManager>.Instance.AddHuntRoles(bdEnums, [WildSpawnType.pmcUSEC, WildSpawnType.pmcBEAR]);
            
            MonoBehaviourSingleton<HuntManager>.Instance.AddHuntSides(bdEnums, new List<EPlayerSide>()
            { 
                EPlayerSide.Usec,
                EPlayerSide.Bear,
            });
            
            var brainList = new List<string>() { "PMC", "ExUsec", "Assault", "PmcUsec", "PmcBear", "PmcUSEC", "PmcBEAR" };
            var typesList = new List<int>() { 848420, 848421, 848422, 848423, 848424, 848426 }.ConvertAll(x => (WildSpawnType)x);

            BrainManager.AddCustomLayer(typeof(HuntTargetLayer), brainList, 10, typesList);
            BrainManager.RemoveLayers(["AdvAssaultTarget"], brainList, typesList);
        }
    }
}
