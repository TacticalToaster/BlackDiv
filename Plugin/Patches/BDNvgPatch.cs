using System.Reflection;
using EFT;
using SPT.Reflection.Patching;

namespace BlackDiv.Patches;

internal class BDNvgPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BotNightVisionData).GetMethod(nameof(BotNightVisionData.MoveToHeadPocket), BindingFlags.Public | BindingFlags.Instance);
    }

    [PatchPrefix]
    protected static bool PatchPostfix(BotNightVisionData __instance)
    {
        if (!WildSpawnTypeExtensions.IsBlackDiv(__instance._owner.Profile.Info.Settings.Role)) return false;

        if (__instance._stopTryingMove) return true;
        
        __instance._stopTryingMove = true;
        __instance.UsingNow = false;
        
        if (__instance.NightVisionItem.Togglable.On)
        {
            __instance._owner.GetPlayer.InventoryController.TryRunNetworkTransaction(__instance.NightVisionItem.Togglable.Set(false, true, false), null);
        }
        
        return true;
    }
}