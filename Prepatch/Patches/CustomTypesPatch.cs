using BepInEx.Logging;
using Mono.Cecil;
using MoreBotsAPI;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace BlackDiv.Prepatch
{
    public static class WildSpawnTypePatch
    {
        public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

        public static void Patch(ref AssemblyDefinition assembly)
        {
            var brains = new List<string>() { "PMC", "ExUsec" };
            var layers = new List<string>() {
                "Request",
                //"FightReqNull",
                //"PeacecReqNull",
                "KnightFight",
                //"PtrlBirdEye",
				"PmcBear",
                "PmcUsec",
                "ExURequest",
                "StationaryWS"
            };

            int baseBrainInt = 24;//9;

            // lead
            var bot = new CustomWildSpawnType(848420, "blackDivLead", "BlackDiv", baseBrainInt, true, true, false);

            bot.SetCountAsBossForStatistics(false);
            bot.SetShouldUseFenceNoBossAttack(false, false);
            bot.SetExcludedDifficulties(new List<int> { 0, 2, 3 });

            SAINSettings settings = new SAINSettings(bot.WildSpawnTypeValue)
            {
                Name = "Black Division Lead",
                Description = "A team leader of Black Division.",
                Section = "Black Division",
                BaseBrain = "PMC",
                BrainsToApply = brains,
                LayersToRemove = layers
            };

            bot.SetSAINSettings(settings);

            CustomWildSpawnTypeManager.RegisterWildSpawnType(bot, assembly);

            // assault
            bot = new CustomWildSpawnType(848421, "blackDivAssault", "BlackDiv", baseBrainInt, true, true, false);

            bot.SetCountAsBossForStatistics(false);
            bot.SetShouldUseFenceNoBossAttack(false, false);
            bot.SetExcludedDifficulties(new List<int> { 0, 2, 3 });

            settings = new SAINSettings(bot.WildSpawnTypeValue)
            {
                Name = "Black Division Assault",
                Description = "An assault member of Black Division, using rifles, carbines, and battle rifles.",
                Section = "Black Division",
                BaseBrain = "PMC",
                BrainsToApply = brains,
                LayersToRemove = layers
            };

            bot.SetSAINSettings(settings);

            CustomWildSpawnTypeManager.RegisterWildSpawnType(bot, assembly);

            // breacher
            bot = new CustomWildSpawnType(848422, "blackDivBreacher", "BlackDiv", baseBrainInt, true, true, false);

            bot.SetCountAsBossForStatistics(false);
            bot.SetShouldUseFenceNoBossAttack(false, false);
            bot.SetExcludedDifficulties(new List<int> { 0, 2, 3 });

            settings = new SAINSettings(bot.WildSpawnTypeValue)
            {
                Name = "Black Division Breacher",
                Description = "A breacher member of Black Division, focusing on close combat.",
                Section = "Black Division",
                BaseBrain = "PMC",
                BrainsToApply = brains,
                LayersToRemove = layers
            };

            bot.SetSAINSettings(settings);

            CustomWildSpawnTypeManager.RegisterWildSpawnType(bot, assembly);

            // support
            bot = new CustomWildSpawnType(848423, "blackDivSupport", "BlackDiv", baseBrainInt, true, true, false);

            bot.SetCountAsBossForStatistics(false);
            bot.SetShouldUseFenceNoBossAttack(false, false);
            bot.SetExcludedDifficulties(new List<int> { 0, 2, 3 });

            settings = new SAINSettings(bot.WildSpawnTypeValue)
            {
                Name = "Black Division Support",
                Description = "A support member of Black Division, using heavy weapons to provide suppression.",
                Section = "Black Division",
                BaseBrain = "PMC",
                BrainsToApply = brains,
                LayersToRemove = layers
            };

            bot.SetSAINSettings(settings);

            CustomWildSpawnTypeManager.RegisterWildSpawnType(bot, assembly);

            CustomWildSpawnTypeManager.AddSuitableGroup(new List<int> { 848420, 848421, 848422, 848423 });
        }

    }
}