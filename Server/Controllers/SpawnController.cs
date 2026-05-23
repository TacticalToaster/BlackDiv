using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Json;

namespace BlackDivServer.Controllers;

[Injectable(InjectionType.Singleton)]
public class SpawnController(
    JsonUtil jsonUtil,
    RandomUtil randomUtil,
    ConfigController configController,
    DatabaseService databaseService,
    RUAFLogger logger,
    HttpResponseUtil httpResponse
)
{
    private BossLocationSpawn? gate1 = null;
    private BossLocationSpawn? gate2 = null;

    private List<string> spawnMaps = ["bigmap", "tarkovstreets", "shoreline", "lighthouse", "factory4_day", "factory4_night", "interchange", "sandbox_high", "rezervbase", "labyrinth"];

    public void AdjustAllSpawns()
    {
        try
        {
            //return;

            var tables = databaseService.GetTables();
            var locations = databaseService.GetLocations();
            var mainConfig = configController.ModConfig;

            var labs = locations.Laboratory;
            labs.Base.BossLocationSpawn.RemoveAll(x => x.BossName.Contains("blackDiv"));

            var gate = labs.Base.BossLocationSpawn.Find(x => x.TriggerId == "autoId_00014_EXFIL");

            if (gate != null)
            {
                gate1 = gate;
            }
            else
            {
                labs.Base.BossLocationSpawn.Add(gate1);
            }

            gate = labs.Base.BossLocationSpawn.Find(x => x.TriggerId == "autoId_00632_EXFIL");

            if (gate != null)
            {
                gate2 = gate;
            }
            else
            {
                labs.Base.BossLocationSpawn.Add(gate2);
            }

            logger.Info("Adding Black Division spawn to Labs.");
            var normalSpawn = new BossLocationSpawn
            {
                BossName = "blackDivAssault",
                BossChance = mainConfig.spawns.labsStartChance,
                BossDifficulty = "normal",
                BossEscortAmount = "2,2,2,3,3,4",
                BossEscortDifficulty = "normal",
                BossEscortType = "blackDivAssault",
                IsBossPlayer = false,
                BossZone = "BotZoneFloor2,BotZoneFloor1,BotZoneBasement",
                Delay = 0,
                ForceSpawn = false,
                IgnoreMaxBots = false,
                IsRandomTimeSpawn = false,
                SpawnMode = ["regular", "pve"],
                Supports = null,
                Time = -1,
                //TriggerId = "byQuest",
                //TriggerName = "byQuest"
            };
            labs.Base.BossLocationSpawn.Add(normalSpawn);
            
            /*var testSpawn = new BossLocationSpawn
            {
                BossName = "bossKilla",
                BossChance = 100,
                BossDifficulty = "normal",
                BossEscortAmount = "1",
                BossEscortDifficulty = "normal",
                BossEscortType = "bossTagilla",
                IsBossPlayer = false,
                BossZone = "BotZoneFloor2,BotZoneFloor1,BotZoneBasement",
                Delay = 0,
                ForceSpawn = false,
                IgnoreMaxBots = false,
                IsRandomTimeSpawn = false,
                SpawnMode = ["regular", "pve"],
                Supports = null,
                Time = -1,
                TriggerId = "5936d90786f7742b1420ba5b",
                TriggerName = "byQuest"
            };
            labs.Base.BossLocationSpawn.Add(testSpawn);*/

            if (randomUtil.GetChance100(mainConfig.spawns.labsGateChances))
            {
                labs.Base.BossLocationSpawn.RemoveAll(x => x.TriggerId == "autoId_00632_EXFIL");
                logger.Info("Adding Black Division EXFIL spawn to Labs.");
                var exfilSpawn = new BossLocationSpawn
                {
                    BossName = "blackDivAssault",
                    BossChance = 100,
                    BossDifficulty = "normal",
                    BossEscortAmount = "3,3,4,5",
                    BossEscortDifficulty = "normal",
                    BossEscortType = "blackDivAssault",
                    IsBossPlayer = false,
                    BossZone = "BotZoneGate1",
                    Delay = 8,
                    ForceSpawn = true,
                    IgnoreMaxBots = true,
                    IsRandomTimeSpawn = false,
                    SpawnMode = ["regular", "pve"],
                    Supports = null,
                    Time = -1,
                    TriggerId = "autoId_00632_EXFIL",
                    TriggerName = "interactObject"
                };
                labs.Base.BossLocationSpawn.Add(exfilSpawn);
                logger.Info("Added Black Division Gate1 spawn to Labs.");
            }

            if (randomUtil.GetChance100(mainConfig.spawns.labsGateChances))
            {
                labs.Base.BossLocationSpawn.RemoveAll(x => x.TriggerId == "autoId_00014_EXFIL");
                logger.Info("Adding Black Division EXFIL spawn to Labs.");
                var exfilSpawn = new BossLocationSpawn
                {
                    BossName = "blackDivAssault",
                    BossChance = 100,
                    BossDifficulty = "normal",
                    BossEscortAmount = "3,3,4,5",
                    BossEscortDifficulty = "normal",
                    BossEscortType = "blackDivAssault",
                    IsBossPlayer = false,
                    BossZone = "BotZoneGate2",
                    Delay = 8,
                    ForceSpawn = true,
                    IgnoreMaxBots = true,
                    IsRandomTimeSpawn = false,
                    SpawnMode = ["regular", "pve"],
                    Supports = null,
                    Time = -1,
                    TriggerId = "autoId_00014_EXFIL",
                    TriggerName = "interactObject"
                };
                labs.Base.BossLocationSpawn.Add(exfilSpawn);
                logger.Info("Added Black Division Gate2 spawn to Labs.");
            }

            foreach (var map in mainConfig.spawns.huntMaps)
            {
                logger.Info($"Adjusting Black Division spawns for {map}.");

                if (!locations.GetDictionary().ContainsKey(locations.GetMappedKey(map)))
                {
                    logger.Info($"No location data found for {map}. Skipping Black Division spawn adjustment.");
                    continue;
                }
                
                var spawns = locations.GetDictionary()[locations.GetMappedKey(map)].Base.BossLocationSpawn;

                // Remove existing spawns
                spawns.RemoveAll(x => x.BossName.Contains("blackDiv"));
                
                AdjustHuntSpawnsForMap(map, spawns, locations.GetDictionary()[locations.GetMappedKey(map)]);
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Error adjusting Black Division spawns: {ex.Message}");
            throw;
        }
    }

    private void AdjustPatrolSpawnsForMap(string map, MainConfig mainConfig, List<BossLocationSpawn> spawns)
    {
        /*var patrolConfig = mapConfig.patrol;

        logger.Info($"Enabling Black Division patrols for {map}.");
        var validZones = new List<string>(patrolConfig.patrolZones);

        for (int i = 0; i < patrolConfig.patrolAmount; i++)
        {
            var patrolSize = randomUtil.GetInt(patrolConfig.patrolMin, patrolConfig.patrolMax);
            var patrol = GeneratePatrol(patrolSize,  100);

            patrol.BossZone = randomUtil.GetArrayValue(validZones);
            validZones.Remove(patrol.BossZone);

            if (validZones.Count == 0)
            {
                validZones = new List<string>(patrolConfig.patrolZones);
            }

            patrol.Time = randomUtil.GetInt(patrolConfig.patrolTimeMin, patrolConfig.patrolTimeMax);

            spawns.Add(patrol);

            logger.Info($"Added ({patrolConfig.patrolChance}% chance) Black Division team of size {patrolSize} to {map} in zone {patrol.BossZone} with a spawn time of {patrol.Time} seconds.");
        }*/
    }

    private void AdjustHuntSpawnsForMap(string map, List<BossLocationSpawn> spawns, Location location)
    {
        spawns.RemoveAll(x => x.TriggerId == "hunt" && x.BossName.Contains("blackDiv"));
        AddHuntToMap(map, spawns, location);
    }

    private void AddHuntToMap(string map, List<BossLocationSpawn> spawns, Location location)
    {
        logger.Info($"Enabling Black Division hunt for {map}.");

        var patrolSize = randomUtil.GetInt(3, 4);
        var patrol = GeneratePatrol(patrolSize, 100, false);

        var factor = randomUtil.GetBiasedRandomNumber(0, 100, 10, 1.5) / 100;
        var timeLimit = (location.Base.EscapeTimeLimit ?? 45) * 60;
        var timeFactor = Double.Lerp(configController.ModConfig.spawns.minTime,
            configController.ModConfig.spawns.maxTime, factor);
        
        patrol.Time = double.Round(timeFactor * timeLimit);
        patrol.BossChance = configController.ModConfig.spawns.chance;

        patrol.BossZone = "";
        patrol.TriggerName = "botEvent";
        patrol.TriggerId = "hunt";
        patrol.ForceSpawn = true;

        spawns.Add(patrol);

        logger.Info($"Added Black Division Hunt of size {patrolSize} to {map} in zone {patrol.BossZone} with a spawn time of {patrol.Time} seconds. {factor} {timeFactor} {timeLimit}");
    }

    private BossLocationSpawn GeneratePatrol(int patrolSize, float chance, bool isPatrol = true)
    {
        var bossType = "blackDivAssault";
        var followers = patrolSize - 1;
        
        var bossInfo = new BossLocationSpawn
        {
            BossChance = chance,
            BossDifficulty = "normal",
            BossEscortAmount = followers.ToString(),
            BossEscortDifficulty = "normal",
            BossEscortType = "blackDivAssault",
            BossName = bossType,
            IsBossPlayer = false,
            BossZone = string.Empty,
            ForceSpawn = false,
            IgnoreMaxBots = true,
            IsRandomTimeSpawn = false,
            SpawnMode = ["regular", "pve"],
            Supports = null,
            Time = -1,
            TriggerId = string.Empty,
            TriggerName = string.Empty
        };

        return bossInfo;
    }
}