# Port Black Division to SPT 4.1.3

Update the client, prepatcher and server to work with the MoreBotsAPI 4.1.3 port while retaining the custom faction/role definitions.

## Implementation notes

- Update .NET 10 server metadata, current namespaces, direct table/config injection and asynchronous loading contracts.
- Retarget bot controller/initialization hooks and client AI movement members to the actual EFT assembly shipped with SPT 4.1.3.
- Fix the NVG prefix: ordinary bot roles must return true so vanilla MoveToHeadPocket still executes. Returning false at that early branch suppresses vanilla behavior for unrelated bots. Black Division adjustments also finish by allowing vanilla processing.
- Use the installed MoreBotsAPI, BigBrain and WTT CommonLib assemblies through SPTPath. Deployment is opt-in.

## Dependencies and validation

The companion MoreBotsAPI PR supplies the SAIN 4.5 custom-role conversion fix that prevented initialization of this faction. No SAIN accuracy/health/difficulty tuning is included here.

Client/server builds and server generation of populated Black Division profiles passed during port testing. The tester completed an Icebreaker raid and later confirmed bots appeared to behave normally. Not all Black Division roles, other locations or multiplayer scenarios have been exercised.

## Compatibility and evidence

Target: **SPT 4.1.3**, EFT **0.16.9.5.40743**. This is a source contribution for that environment, not a claim of compatibility with future SPT releases or Fika.

The tester successfully loaded Icebreaker, entered a raid and extracted. In subsequent feedback they confirmed the blowtorch, extraction and doors work, and Black Division bots appeared to behave normally after the SAIN fix. These are user-reported functional observations, not automated coverage of every encounter or performance benchmarks. Ten continuous hours, all quests and multiplayer have not been tested.

The migration used installed SPT 4.1.3 assemblies and these guides:
- https://wiki.sp-tushonka.com/en/modding/SPT_41_Modding/Server_413_Changes
- https://wiki.sp-tushonka.com/en/modding/SPT_41_Modding/client/Class_Name_Mappings
- https://wiki.sp-tushonka.com/en/modding/SPT_41_Modding/server/Mod_Web_Pages

## Build and packaging

Use the .NET 10 SDK and an installed SPT 4.1.3 dependency set. Pass `-p:SPTPath=<installation-root>` to dotnet build; the fallback expects this repository under a development tree. DeployToGame defaults to disabled. Build the client and server projects in Release, and the companion dependency ports first. Proprietary game DLLs are local references and must not be committed.


## Contribution build checks

The publication working copies were built in Release against the installed SPT 4.1.3 assemblies with deployment disabled. Client/server builds passed for Icebreaker, MoreBotsAPI, BlackDiv and ManimalCSGas; the Backport prepatcher and DynamicMaps client also passed. Existing compiler warnings remain in several ports. Fika was not built as part of this contribution gate. These checks validate compilation, not untested gameplay.
