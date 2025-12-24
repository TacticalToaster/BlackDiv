using EFT;
using BlackDiv.Components;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BlackDiv.Patches
{
    internal class BotsControllerInitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotsController).GetMethod(nameof(BotsController.Init), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(BotsController __instance)
        {
            Plugin.LogSource.LogInfo("BotsController initialized, initializing Managers...");
            //MonoBehaviourSingleton<RuafCheckpointManager>.Instance.InitRaid();
            MonoBehaviourSingleton<HuntManager>.Instance.InitRaid();
        }
    }
}
