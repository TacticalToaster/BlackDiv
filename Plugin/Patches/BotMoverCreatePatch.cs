#if !UNITY_EDITOR
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using EFT.InputSystem;
using MoreBotsAPI.Behavior.Layers;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;

namespace BlackDiv.Patches
{
    internal class BotMoverCreatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotMover).GetMethod(nameof(BotMover.Create), BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPrefix]
        protected static bool PatchPrefix(BotOwner owner, AICoversData covers, ref BotMover __result)
        {
            if (owner.Profile.Info.Settings.Role == (WildSpawnType)848425)
            {
                Plugin.LogSource.LogWarning("Heli Gunner Mover!");
                __result = new HeliGunnerMover(owner, owner.GetPlayer, covers);
                return false;
            }

            return true;
        }
    }
}
#endif
