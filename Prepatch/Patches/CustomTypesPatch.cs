using Mono.Cecil;
using MoreBotsAPI;
using System.Collections.Generic;

namespace BlackDiv.Prepatch
{
    public static class WildSpawnTypePatch
    {
        private const int BaseBrainType = 9;

        private const int LeadId = 848420;
        private const int AssaultId = 848421;
        private const int BreacherId = 848422;
        private const int SupportId = 848423;
        private const int WedgeId = 848424;
        private const int IbId = 848426;

        private static readonly List<int> ExcludedDifficulties = new()
        {
            0,
            2,
            3
        };

        private static readonly List<int> BlackDivGroup = new()
        {
            LeadId,
            AssaultId,
            BreacherId,
            SupportId,
            WedgeId,
            IbId
        };

        public static IEnumerable<string> TargetDLLs { get; } = new[]
        {
            "Assembly-CSharp.dll"
        };

        public static void Patch(ref AssemblyDefinition assembly)
        {
            RegisterBot(assembly, LeadId, "blackDivLead", "BlackDiv");
            RegisterBot(assembly, AssaultId, "blackDivAssault", "BlackDiv");
            RegisterBot(assembly, BreacherId, "blackDivBreacher", "BlackDiv");
            RegisterBot(assembly, SupportId, "blackDivSupport", "BlackDiv");

            RegisterBot(
                assembly,
                WedgeId,
                "bossWedge",
                "Boss",
                countsAsBoss: true);

            RegisterBot(assembly, IbId, "blackDivIb", "BlackDiv");

            CustomWildSpawnTypeManager.AddSuitableGroup(BlackDivGroup);
        }

        private static void RegisterBot(
            AssemblyDefinition assembly,
            int id,
            string name,
            string role,
            bool countsAsBoss = false)
        {
            var bot = new CustomWildSpawnType(
                id,
                name,
                role,
                BaseBrainType,
                true,
                true,
                false);

            bot.SetCountAsBossForStatistics(countsAsBoss);
            bot.SetShouldUseFenceNoBossAttack(false, false);
            bot.SetExcludedDifficulties(ExcludedDifficulties);

            CustomWildSpawnTypeManager.RegisterWildSpawnType(bot, assembly);
        }
    }
}