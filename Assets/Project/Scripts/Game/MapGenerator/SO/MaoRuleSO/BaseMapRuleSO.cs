using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public struct ChunkGenParams
{
    public Tilemap globalTilemap;
    public int[,] mapData;
    public int offsetX;
    public int startY;
    public float seed;
    public Vector2Int startPlatform;
    public List<GameObject> spawnedList;
}

[System.Serializable]
public struct SpawnRule
{
    public GameObject prefab;
    [Range(0f, 1f)] public float spawnChance;
}

public abstract class BaseMapRuleSO : ScriptableObject
{
    [Header("공통 맵 크기 설정")]
    public int chunkWidth = 300;
    public int chunkHeight = 50;

    [Header("테마별 타일 셋 (1번부터 순서대로 인스펙터 매핑)")]
    public List<TileBase> themeTiles = new List<TileBase>();

    [Header("오브젝트/몬스터 스폰 설정")]
    public List<SpawnRule> platformSpawns; // 플랫폼 위 스폰
    public List<SpawnRule> groundSpawns;   // 일반 바닥 스폰
    public List<SpawnRule> ceilingSpawns;  // 천장 스폰
    [Tooltip("몬스터 간 최소 X축 스폰 간격 (타일 수)")]
    public int minSpawnGapX = 8;

    [System.NonSerialized] protected Tilemap globalTilemap;
    [System.NonSerialized] protected int[,] mapData;
    [System.NonSerialized] protected int offsetX;
    [System.NonSerialized] protected float currentSeed;
    [System.NonSerialized] protected List<GameObject> spawnedList;
    [System.NonSerialized] protected System.Random chunkRandom;

    protected List<Vector2Int> mainPath = new List<Vector2Int>();

    /// <summary>
    /// 전체 청크 생성 파이프라인의 실행 순서를 보장
    /// </summary>
    public int GenerateChunk(ChunkGenParams genParams, out Vector2Int endPlatform)
    {
        this.globalTilemap = genParams.globalTilemap;
        this.mapData = genParams.mapData;
        this.offsetX = genParams.offsetX;
        this.currentSeed = genParams.seed;
        this.spawnedList = genParams.spawnedList;

        this.chunkRandom = new System.Random((int)currentSeed);
        Random.InitState((int)currentSeed);
        mainPath.Clear();

        InitializeMap();
        int exitY = CarveTerrain(genParams.startY);

        ForceTransitionTunnel(genParams.startY);
        endPlatform = PlacePlatforms(genParams.startPlatform);

        RenderToTilemap();
        
        SpawnObjects();

        return exitY;
    }

    protected virtual void InitializeMap()
    {
        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkHeight; y++) mapData[x, y] = 1;
        }
    }

    private void RenderToTilemap()
    {
        TileBase[] tileArray = new TileBase[chunkWidth * chunkHeight];
        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                int tileID = mapData[x, y];
                int index = x + y * chunkWidth;
                tileArray[index] = (tileID > 0 && tileID <= themeTiles.Count) ? themeTiles[tileID - 1] : null;
            }
        }
        BoundsInt bounds = new BoundsInt(offsetX, 0, 0, chunkWidth, chunkHeight, 1);
        globalTilemap.SetTilesBlock(bounds, tileArray);
    }

    private void ForceTransitionTunnel(int startY)
    {
        if (mainPath.Count == 0) return;

        int targetY = mainPath[0].y;
        int transitionLength = 15;

        for (int x = 0; x < transitionLength; x++)
        {
            float t = (float)x / transitionLength;
            int currentY = Mathf.RoundToInt(Mathf.Lerp(startY, targetY, t));
            int radius = 5;

            for (int cx = -radius; cx <= radius; cx++)
            {
                for (int cy = -radius; cy <= radius; cy++)
                {
                    if (cx * cx + cy * cy <= radius * radius)
                    {
                        int px = x + cx;
                        int py = currentY + cy;
                        if (px >= 0 && px < chunkWidth && py >= 0 && py < chunkHeight)
                        {
                            if (mapData[px, py] == 1) mapData[px, py] = 0;
                        }
                    }
                }
            }
        }
    }

    private void SpawnObjects()
    {
        if (spawnedList == null) return;

        int lastPlatformSpawnX = -minSpawnGapX;
        int lastGroundSpawnX = -minSpawnGapX;
        int lastCeilingSpawnX = -minSpawnGapX;

        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                // 플랫폼 위 검사
                if (y < chunkHeight - 1 && mapData[x, y] == 2 && mapData[x, y + 1] == 0)
                {
                    if (x - lastPlatformSpawnX >= minSpawnGapX)
                    {
                        if (TrySpawnObject(platformSpawns, x, y + 1))
                        {
                            lastPlatformSpawnX = x;
                        }
                    }
                }
                // 바닥 검사
                else if (y < chunkHeight - 1 && mapData[x, y] == 1 && mapData[x, y + 1] == 0)
                {
                    if (x - lastGroundSpawnX >= minSpawnGapX)
                    {
                        if (TrySpawnObject(groundSpawns, x, y + 1))
                        {
                            lastGroundSpawnX = x;
                        }
                    }
                }

                // 천장 검사
                if (y > 0 && mapData[x, y] == 1 && mapData[x, y - 1] == 0)
                {
                    if (x - lastCeilingSpawnX >= minSpawnGapX)
                    {
                        if (TrySpawnObject(ceilingSpawns, x, y - 1))
                        {
                            lastCeilingSpawnX = x;
                        }
                    }
                }
            }
        }
    }

    private bool TrySpawnObject(List<SpawnRule> rules, int localX, int localY)
    {
        if (rules == null || rules.Count == 0) return false;

        int randomIdx = chunkRandom.Next(0, rules.Count);
        SpawnRule rule = rules[randomIdx];

        if ((float)chunkRandom.NextDouble() > rule.spawnChance) return false;

        Vector3Int cellPos = new Vector3Int(offsetX + localX, localY, 0);
        Vector3 worldPos = globalTilemap.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0);

        GameObject instance = Managers.PoolManager.Instance.Pop(rule.prefab, worldPos, Quaternion.identity);
        if (instance != null)
        {
            // 원본 프리팹은 건드리지 않고, 스폰된 인스턴스에만 런타임에 동적으로 컴포넌트 추가
            var mapObj = instance.GetComponent<MapSpawnedObject>();
            if (mapObj == null)
            {
                mapObj = instance.AddComponent<MapSpawnedObject>();
            }
            mapObj.poolKey = rule.prefab.GetInstanceID();

            spawnedList.Add(instance);
            return true;
        }

        return false;
    }

    protected abstract int CarveTerrain(int startY);
    protected abstract Vector2Int PlacePlatforms(Vector2Int startPlatform);
}