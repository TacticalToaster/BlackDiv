using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using System.Reflection;

namespace BlackDivServer;

[Injectable(InjectionType.Singleton)]
public class ConfigController
{
    public MainConfig ModConfig;
    
    private FileSystemWatcher watcher;

    public readonly ModHelper _modHelper;
    
    public RUAFLogger logger;

    public ConfigController(ModHelper modHelper)
    {
        _modHelper = modHelper;

        var pathToMod = _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

        ModConfig = _modHelper.GetJsonDataFromFile<MainConfig>(pathToMod, "config.jsonc");
        
        watcher = new FileSystemWatcher(pathToMod,  "config.jsonc");
        watcher.NotifyFilter = NotifyFilters.LastWrite;

        watcher.Changed += OnChanged;
        
        watcher.EnableRaisingEvents = true;
    }

    public void OnChanged(object sender, FileSystemEventArgs e)
    {
        ReloadConfig();
    }

    public void ReloadConfig()
    {
        Task.Run(async () =>
            {
                await Task.Delay(300);

                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        var pathToMod = _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

                        ModConfig = _modHelper.GetJsonDataFromFile<MainConfig>(pathToMod, "config.jsonc");
                        return;
                    }
                    catch (IOException)
                    {
                        await Task.Delay(300);
                    }
                }
            }
        );
        
        logger?.Info("Config reloaded.", true);
    }
}