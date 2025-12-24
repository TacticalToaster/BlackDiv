namespace BlackDivServer;

public class MainConfig
{
    public DebugConfig debug { get; set; }
    public PatrolConfig patrols { get; set; }
    public PatrolConfig checkpoints { get; set; }

    public Dictionary<string, MapConfig> locations { get; set; }
}

public class DebugConfig
{
    public bool logs { get; set; }
    public bool spawnAlways { get; set; }
    public bool spawnInstantlyAlways { get; set; }
}

public class PatrolConfig
{
    public int minSecondLeaderSize { get; set; }
    public float secondLeaderChance { get; set; }
    public int specialistEveryPerson { get; set; }
}

public class MapPatrolConfig
{
    public bool enablePatrols { get; set; }
    public float patrolChance { get; set; }
    public int patrolAmount { get; set; }
    public int patrolMin { get; set; }
    public int patrolMax { get; set; }
    public List<string> patrolZones { get; set; }
    public int patrolTimeMin { get; set; }
    public int patrolTimeMax { get; set; }
}

public class MapCheckpointConfig
{
    public bool enableCheckpoints { get; set; }
    public int checkpointAmount { get; set; }
    public List<ZoneCheckpointConfig> checkpointZones { get; set; }
}

public class ZoneCheckpointConfig
{
    public string checkpointZone { get; set; }
    public float checkpointChance { get; set; }
    public int checkpointMin { get; set; }
    public int checkpointMax { get; set; }
    public float checkpointRadius { get; set; }
    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }
}

public class MapHuntConfig
{
    public bool enableHunts { get; set; }
    public Dictionary<string, FactionHuntConfig> hunts { get; set; }
}

public class FactionHuntConfig
{
    public int huntMin { get; set; }
    public int huntMax { get; set; }
    public string huntZones { get; set; }
}

public class MapConfig
{
    public MapPatrolConfig patrol { get; set; }
    public MapCheckpointConfig checkpoint { get; set; }
    public MapHuntConfig hunt { get; set; }
}