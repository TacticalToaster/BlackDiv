using DrakiaXYZ.BigBrain.Brains;
using EFT;
using EFT.InputSystem;
using BlackDiv.Behavior.Layers;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;

namespace BlackDiv.Patches
{
    internal class TarkovInitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TarkovApplication).GetMethod(nameof(TarkovApplication.Init), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(IAssetsManager assetsManager, InputTree inputTree)
        {
            var brainList = new List<string>() { "PMC", "ExUsec", "Assault", "PmcUsec", "PmcBear", "PmcUSEC", "PmcBEAR" };
            var typesList = new List<int>() { 848420, 848421, 848422, 848423, 848424 }.ConvertAll(x => (WildSpawnType)x);

            BrainManager.AddCustomLayer(typeof(HuntTargetLayer), brainList, 5, typesList);
        }
    }
}
