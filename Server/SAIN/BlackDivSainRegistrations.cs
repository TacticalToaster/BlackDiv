using MoreBotsServer.Interop;
using MoreBotsServer.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using System.Collections.Generic;

namespace BlackDivServer.SAIN;

[Injectable(TypePriority = OnLoadOrder.Preload + 2)]
public sealed class BlackDivSainRegistrations (SainInteropRegistration sainInterop) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        RegisterBLackDivision();
        return Task.CompletedTask;
    }
    private void RegisterBLackDivision()
    {
        sainInterop.RegisterBotType(CreateRegistration(
            wildSpawnType: 848420,
            botDbKey: "blackDivLead",
            name: "Black Division Lead",
            description: "A team leader of Black Division."));

        sainInterop.RegisterBotType(CreateRegistration(
            wildSpawnType: 848421,
            botDbKey: "blackDivAssault",
            name: "Black Division Assault",
            description: "An assault member of Black Division, using rifles, carbines, and battle rifles."));

        sainInterop.RegisterBotType(CreateRegistration(
            wildSpawnType: 848422,
            botDbKey: "blackDivBreacher",
            name: "Black Division Breacher",
            description: "A breacher member of Black Division, focusing on close combat."));

        sainInterop.RegisterBotType(CreateRegistration(
            wildSpawnType: 848423,
            botDbKey: "blackDivSupport",
            name: "Black Division Support",
            description: "A support member of Black Division, using heavy weapons to provide suppression."));

        sainInterop.RegisterBotType(CreateRegistration(
            wildSpawnType: 848424,
            botDbKey: "bossWedge",
            name: "Wedge",
            description: "A hyper-lethal leader within Black Division."));

        sainInterop.RegisterBotType(CreateRegistration(
            wildSpawnType: 848426,
            botDbKey: "blackDivIb",
            name: "Black Division Raider",
            description: "A member of Black Division that is a part of a raiding party."));
    }

    private static MoreBotsSainBotTypeRegistration CreateRegistration(
        int wildSpawnType,
        string botDbKey,
        string name,
        string description)
    {
        return new MoreBotsSainBotTypeRegistration
        {
            WildSpawnType = wildSpawnType,
            BotDbKey = botDbKey,
            Name = name,
            Description = description,
            Section = "Black Division",
            DifficultyModifier = 1f,

            BrainsToApply =
            [
                "PMC",
                "ExUsec",
            ],

            LayersToRemove =
            [
                "Request",
                "KnightFight",
                "PmcBear",
                "PmcUsec",
                "ExURequest",
                "StationaryWS",
            ],
        };
    }
}