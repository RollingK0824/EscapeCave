using UnityEngine;

public enum MonsterZoneType { None, Ceiling, Ground, Water}

public class MonsterSpawnArea : MonoBehaviour
{
    public MonsterZoneType ZoneType;

    public float MinX;
    public float MaxX;
    public float MinY;
    public float MaxY;
}
