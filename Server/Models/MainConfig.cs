namespace BlackDivServer;

public class MainConfig
{
    public DebugConfig debug { get; set; }
    public SpawnConfig spawns { get; set; }
}

public class DebugConfig
{
    public bool logs { get; set; }
}

public class SpawnConfig
{
    public float chance { get; set; }
    public float minTime { get; set; }
    public float maxTime { get; set; }
    public float labsGateChances { get; set; }
    public float labsStartChance { get; set; }
    public List<string> huntMaps { get; set; }
}