using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;

namespace BlackDivServer;

[Injectable(InjectionType.Singleton)]
public class RUAFLogger
{
    private readonly bool _enableLogs;
    private readonly ISptLogger<RUAFLogger> _logger;
    public RUAFLogger(
        ISptLogger<RUAFLogger> logger,
        ConfigController configController)
    {
        _enableLogs = true;
        _logger = logger;
    }

    public void Info(string message)
    {
        if (_enableLogs)
        {
            _logger.Info($"[BlackDiv Mod] {message}");
        }
    }
    public void Warn(string message)
    {
        _logger.Warning($"[BlackDiv Mod] WARNING: {message}");
    }
    public void Error(string message)
    {
        _logger.Error($"[BlackDiv Mod] ERROR: {message}");
    }
}