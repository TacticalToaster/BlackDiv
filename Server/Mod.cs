using MoreBotsServer.Models;
using BlackDivServer.Controllers;
using SPTarkov.Common.Extensions;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using System.Reflection;

namespace BlackDivServer;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.blackdiv.tacticaltoaster";
    public override string Name { get; init; } = "Black Division [REDACTED] Home";
    public override string Author { get; init; } = "TacticalToaster";
    public override List<string>? Contributors { get; init; } = new() { };
    public override SemanticVersioning.Version Version { get; init; } = new(1, 0, 1);
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; } = new()
    {
        { "com.morebotsapi.tacticaltoaster", new SemanticVersioning.Range(">=1.0.0") },
        { "com.wtt.commonlib", new SemanticVersioning.Range(">=2.0.0") },
        { "com.wtt.contentbackport",  new SemanticVersioning.Range(">=1.0.0") }
    };
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; }
    public override string License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader + 1)]
public class ModPreload : IOnLoad
{
    public static MainConfig ModConfig = new();

    private readonly ModHelper _modHelper;

    public ModPreload(
        ModHelper modHelper
        )
    {
        _modHelper = modHelper;
    }

    Task IOnLoad.OnLoad()
    {
        var pathToMod = _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

        //ModConfig = _modHelper.GetJsonDataFromFile<MainConfig>(pathToMod, "config.jsonc");

        return Task.CompletedTask;
    }
}

[Injectable(InjectionType = InjectionType.Singleton, TypePriority = MoreBotsServer.MoreBotsLoadOrder.LoadBots)]
public class BlackDivServer(
    MoreBotsServer.MoreBotsAPI moreBotsLib,
    MoreBotsServer.Services.MoreBotsCustomBotTypeService customBotTypeService,
    MoreBotsServer.Services.FactionService factionService,
    MoreBotsServer.Services.LoadoutService loadoutService,
    WTTServerCommonLib.WTTServerCommonLib commonLib,
    IReadOnlyList<SptMod> modList,
    SpawnController spawnController
) : IOnLoad
{
    public async Task OnLoad()
    {
        var typeList = new List<string> {
            "blackDivLead",
            "blackDivAssault",
            "blackDivBreacher",
            "blackDivSupport"
        };

        var typeDictionary = new Dictionary<int, string>()
        {
            { 848420, "blackDivLead" },
            { 848421, "blackDivAssault" },
            { 848422, "blackDivBreacher" },
            { 848423, "blackDivSupport" }
        };

        var assembly = Assembly.GetExecutingAssembly();

        // Load base bot types using a shared type
        await moreBotsLib.LoadBotsShared(assembly, "blackDiv", typeList);

        await commonLib.CustomBotLoadoutService.CreateCustomBotLoadouts(assembly);

        if (modList.Any(mod => mod.ModMetadata.ModGuid == "com.wtt.armory"))
        {
            await commonLib.CustomBotLoadoutService.CreateCustomBotLoadouts(assembly,
                Path.Join("db", "ModBotLoadouts", "Armory"));
        }

        customBotTypeService.AddCustomWildSpawnTypeNames(typeDictionary);

        // Add enemies based on factions
        factionService.AddEnemyByFaction(typeList, "savage");
        factionService.AddEnemyByFaction(typeList, "rogues");
        factionService.AddEnemyByFaction(typeList, "usec");
        factionService.AddEnemyByFaction(typeList, "bear");
        factionService.AddEnemyByFaction(typeList, "infected");

        // Add RUAF as enemies to those same factions
        factionService.AddEnemyByFaction("savage", "blackdiv");
        factionService.AddEnemyByFaction("rogues", "blackdiv");
        factionService.AddEnemyByFaction("usec", "blackdiv");
        factionService.AddEnemyByFaction("bear", "blackdiv");
        factionService.AddEnemyByFaction("infected", "blackdiv");

        factionService.AddRevengeByFaction(typeList, "blackdiv");

        if (modList.Any(mod => mod.ModMetadata.ModGuid == "com.ruafcomehome.tacticaltoaster"))
        {
            factionService.AddEnemyByFaction(typeList, "ruaf");
        }

        //await commonLib.CustomAchievementService.CreateCustomAchievements(assembly);

        // Use WTT to add locales
        await commonLib.CustomLocaleService.CreateCustomLocales(assembly);

        // Add to spawns
        spawnController.AdjustAllSpawns();

        await Task.CompletedTask;
    }
}

[Injectable(InjectionType = InjectionType.Singleton, TypePriority = MoreBotsServer.MoreBotsLoadOrder.LoadFactions)]
public class BlackDivFaction(
    MoreBotsServer.Services.FactionService factionService
) : IOnLoad
{
    public async Task OnLoad()
    {
        // Create the new BlackDiv faction
        factionService.Factions.Add("blackdiv", new Faction()
        {
            Name = "blackdiv",
            BotTypes =
            {
                (WildSpawnType)848420,
                (WildSpawnType)848421,
                (WildSpawnType)848422,
                (WildSpawnType)848423
            }
        });

        await Task.CompletedTask;
    }
}

[Injectable]
public class CustomDynamicRouter : DynamicRouter
{
    private static HttpResponseUtil _httpResponseUtil;
    private static ConfigController _configController;

    public CustomDynamicRouter(
        JsonUtil jsonUtil,
        HttpResponseUtil httpResponseUtil,
        ConfigController configController) : base(jsonUtil, GetCustomRoutes())
    {
        _httpResponseUtil = httpResponseUtil;
        _configController = configController;
    }
    private static List<RouteAction> GetCustomRoutes()
    {
        return [
            new RouteAction(
                "/blackDiv/checkpoints",
                async (
                    url,
                    info,
                    sessionID,
                    output
                ) => {
                    var result = _configController.ModConfig;
                    return await new ValueTask<string>(_httpResponseUtil.NoBody(result));
                }
            )
        ];
    }
}

[Injectable]
public class CustomStaticRouter : StaticRouter
{
    private static HttpResponseUtil _httpResponseUtil;
    private static SpawnController _spawnController;

    public CustomStaticRouter(
        SpawnController untarSpawnController,
        JsonUtil jsonUtil,
        HttpResponseUtil httpResponseUtil) : base(jsonUtil, GetCustomRoutes())
    {
        _httpResponseUtil = httpResponseUtil;
        _spawnController = untarSpawnController;
    }

    private static List<RouteAction> GetCustomRoutes()
    {
        return
        [
            new RouteAction(
                "/client/match/local/end",
                async (
                    url,
                    info,
                    sessionID,
                    output
                ) => {
                    _spawnController.AdjustAllSpawns();
                    return await new ValueTask<object>(output ?? string.Empty);
                }
            )
        ];
    }
}