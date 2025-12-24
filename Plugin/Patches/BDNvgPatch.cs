using System.Reflection;
using BlackDiv.Components;
using EFT;
using SPT.Reflection.Patching;

namespace BlackDiv.Patches;

internal class BDNvgPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BotNightVisionData).GetMethod(nameof(BotNightVisionData.method_1), BindingFlags.Public | BindingFlags.Instance);
    }

    [PatchPrefix]
    protected static bool PatchPostfix(BotNightVisionData __instance)
    {
        if (!WildSpawnTypeExtensions.IsBlackDiv(__instance.BotOwner_0.Profile.Info.Settings.Role)) return false;

        if (__instance.StopTryingMove) return true;
        
        __instance.StopTryingMove = true;
        __instance.UsingNow = false;
        
        if (__instance.NightVisionItem.Togglable.On)
        {
            __instance.BotOwner_0.GetPlayer.InventoryController.TryRunNetworkTransaction(__instance.NightVisionItem.Togglable.Set(false, true, false), null);
        }
        
        return true;
    }
}