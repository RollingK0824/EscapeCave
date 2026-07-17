using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public struct ChunkGenParams
{
    public Tilemap globalTilemap;
    public Tilemap waterTilemap;
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

[System.Serializable]
public enum PlatformType
{
    Static,
    Moving,
    Pullable
}

[System.Serializable]
public enum MoveDirection
{
    Horizontal,
    Vertical
}

[System.Serializable]
public struct PlatformSpawnRule
{
    public GameObject prefab;
    public PlatformType type;
    public int minLength;
    public int maxLength;
    [Range(0f, 1f)] public float spawnChance;
    
    public MoveDirection moveDirection;
    public int moveRange;
    public float speed;
    
    public float pullLimit;
}

public abstract class BaseMapRuleSO : ScriptableObject
{
    [Header("공통 맵 크기 설정")]
    public int chunkWidth = 300;
    public int chunkHeight = 50;
    public int blendRange = 30;

    [Header("테마별 타일 셋 (1번부터 순서대로 인스펙터 매핑)")]
    public List<TileBase> themeTiles = new List<TileBase>();

    [Header("오브젝트/몬스터 스폰 설정")]
    public List<PlatformSpawnRule> platformConfigurations; 
    public List<SpawnRule> groundSpawns;   
    public List<SpawnRule> platformSpawns;   
    public List<SpawnRule> ceilingSpawns;  
    public List<SpawnRule> underPlatformSpawns; 
    [Tooltip("몬스터 간 최소 X축 스폰 간격 (타일 수)")]
    public int minSpawnGapX = 8;

    [System.NonSerialized] protected Tilemap globalTilemap;
    [System.NonSerialized] protected Tilemap waterTilemap;
    [System.NonSerialized] protected int[,] mapData;
    [System.NonSerialized] protected int offsetX;
    [System.NonSerialized] protected float currentSeed;
    [System.NonSerialized] protected List<GameObject> spawnedList;
    [System.NonSerialized] protected System.Random chunkRandom;

    protected List<Vector2Int> mainPath = new List<Vector2Int>();
    
    protected struct PlatformSpawnData
    {
        public int localX;
        public int localY;
        public int chosenLength;
        public PlatformSpawnRule rule;
    }
    protected List<PlatformSpawnData> pendingPlatforms = new List<PlatformSpawnData>();

    public int GenerateChunk(ChunkGenParams genParams, out Vector2Int endPlatform)
    {
        this.globalTilemap = genParams.globalTilemap;
        this.waterTilemap = genParams.waterTilemap;
        this.mapData = genParams.mapData;
        this.offsetX = genParams.offsetX;
        this.currentSeed = genParams.seed;
        this.spawnedList = genParams.spawnedList;

        this.chunkRandom = new System.Random((int)currentSeed);
        Random.InitState((int)currentSeed);
        mainPath.Clear();
        pendingPlatforms.Clear();

        InitializeMap();
        int exitY = CarveTerrain(genParams.startY);

        SmoothBoundaries(genParams.startY, exitY);
        
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

    protected void SmoothBoundaries(int startY, int exitY)
    {
        int tunnelRadius = 6;
        
        // 입구 보간
        for (int x = 0; x < blendRange; x++)
        {
            float t = (float)x / blendRange;
            int currentFloor = GetFloorY(x);
            int startFloor = startY - tunnelRadius;
            int smoothedFloor = Mathf.RoundToInt(Mathf.Lerp(startFloor, currentFloor, t));
            
            int currentCeil = GetCeilY(x);
            int startCeil = startY + tunnelRadius;
            int smoothedCeil = Mathf.RoundToInt(Mathf.Lerp(startCeil, currentCeil, t));
            
            ApplySmoothedColumn(x, smoothedFloor, smoothedCeil);
        }

        // 출구 보간
        for (int x = chunkWidth - blendRange; x < chunkWidth; x++)
        {
            float t = (float)(x - (chunkWidth - blendRange)) / blendRange;
            
            int currentFloor = GetFloorY(x);
            int exitFloor = exitY - tunnelRadius;
            int smoothedFloor = Mathf.RoundToInt(Mathf.Lerp(currentFloor, exitFloor, t));
            
            int currentCeil = GetCeilY(x);
            int exitCeil = exitY + tunnelRadius;
            int smoothedCeil = Mathf.RoundToInt(Mathf.Lerp(currentCeil, exitCeil, t));
            
            ApplySmoothedColumn(x, smoothedFloor, smoothedCeil);
        }
    }

    private int GetFloorY(int x)
    {
        for (int y = 0; y < chunkHeight; y++)
        {
            if (mapData[x, y] == 0 || mapData[x, y] == 3) return y;
        }
        return 0;
    }

    private int GetCeilY(int x)
    {
        for (int y = chunkHeight - 1; y >= 0; y--)
        {
            if (mapData[x, y] == 0 || mapData[x, y] == 3) return y;
        }
        return chunkHeight - 1;
    }

    private void ApplySmoothedColumn(int x, int floorY, int ceilY)
    {
        for (int y = 0; y < chunkHeight; y++)
        {
            if (y < floorY) mapData[x, y] = 1;
            else if (y > ceilY) mapData[x, y] = 1;
            else
            {
                if (mapData[x, y] == 1) mapData[x, y] = 0;
            }
        }
    }

    protected virtual void RenderToTilemap()
    {
        TileBase[] tileArray = new TileBase[chunkWidth * chunkHeight];
        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                int tileID = mapData[x, y];
                int index = x + y * chunkWidth;
                if (tileID == 2 || tileID == 4 || tileID == 5) tileID = 0;
                
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

        int lastGroundSpawnX = -minSpawnGapX;
        int lastCeilingSpawnX = -minSpawnGapX;
        int lastUnderPlatformSpawnX = -minSpawnGapX;
        int lastPlatformSpawnX = -minSpawnGapX;

        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                // 바닥 검사
                if (y < chunkHeight - 1 && mapData[x, y] == 1 && mapData[x, y + 1] == 0)
                {
                    if (x - lastGroundSpawnX >= minSpawnGapX)
                    {
                        if (TrySpawnObject(groundSpawns, x, y + 1)) lastGroundSpawnX = x;
                    }
                }

                // 천장 검사
                if (y > 0 && mapData[x, y] == 1 && mapData[x, y - 1] == 0)
                {
                    if (x - lastCeilingSpawnX >= minSpawnGapX)
                    {
                        if (TrySpawnObject(ceilingSpawns, x, y - 1)) lastCeilingSpawnX = x;
                    }
                }

                // 플랫폼 밑 검사
                if (y > 0 && (mapData[x, y] == 2 || mapData[x, y] == 4 || mapData[x, y] == 5) && mapData[x, y - 1] == 0)
                {
                    if (x - lastUnderPlatformSpawnX >= minSpawnGapX)
                    {
                        if (TrySpawnObject(underPlatformSpawns, x, y - 1)) lastUnderPlatformSpawnX = x;
                    }
                }

                // 플랫폼 위 검사
                if (y < chunkHeight - 1 && (mapData[x, y] == 2 || mapData[x, y] == 4 || mapData[x, y] == 5) && mapData[x, y + 1] == 0)
                {
                    if (x - lastPlatformSpawnX >= minSpawnGapX)
                    {
                        if (TrySpawnObject(platformSpawns, x, y + 1)) lastPlatformSpawnX = x;
                    }
                }
            }
        }

        foreach (var data in pendingPlatforms)
        {
            SpawnPlatform(data);
        }
    }

    protected bool TrySpawnObject(List<SpawnRule> rules, int localX, int localY)
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
            var mapObj = instance.GetComponent<MapSpawnedObject>();
            if (mapObj == null) mapObj = instance.AddComponent<MapSpawnedObject>();
            mapObj.poolKey = rule.prefab.GetInstanceID();

            spawnedList.Add(instance);
            return true;
        }

        return false;
    }

    private void SpawnPlatform(PlatformSpawnData data)
    {
        if (data.rule.prefab == null) return;

        Vector3Int cellPos = new Vector3Int(offsetX + data.localX, data.localY, 0);
        // 중심 맞추기 (결정된 플랫폼 타일 폭 절반만큼 이동)
        Vector3 worldPos = globalTilemap.CellToWorld(cellPos) + new Vector3(0.5f + (data.chosenLength - 1) * 0.5f, 0.5f, 0);

        GameObject instance = Managers.PoolManager.Instance.Pop(data.rule.prefab, worldPos, Quaternion.identity);
        if (instance != null)
        {
            // 1. SpriteRenderer 리사이징 (9-Slice 대응)
            var spriteRenderer = instance.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                if (spriteRenderer.drawMode == SpriteDrawMode.Sliced || spriteRenderer.drawMode == SpriteDrawMode.Tiled)
                {
                    spriteRenderer.size = new Vector2(data.chosenLength, spriteRenderer.size.y);
                }
                else
                {
                    Vector3 scale = instance.transform.localScale;
                    scale.x = data.chosenLength;
                    instance.transform.localScale = scale;
                }
            }

            // 2. BoxCollider2D 물리 영역 리사이징
            var boxCollider = instance.GetComponentInChildren<BoxCollider2D>();
            if (boxCollider != null)
            {
                boxCollider.size = new Vector2(data.chosenLength, boxCollider.size.y);
            }

            var mapObj = instance.GetComponent<MapSpawnedObject>();
            if (mapObj == null) mapObj = instance.AddComponent<MapSpawnedObject>();
            mapObj.poolKey = data.rule.prefab.GetInstanceID();

            if (data.rule.type == PlatformType.Moving)
            {
                var movingPlatform = instance.GetComponent<MovingPlatform>();
                if (movingPlatform == null) movingPlatform = instance.AddComponent<MovingPlatform>();
                movingPlatform.Initialize(data.rule.moveDirection, data.rule.moveRange, data.rule.speed);
            }
            else if (data.rule.type == PlatformType.Pullable)
            {
                var pullablePlatform = instance.GetComponent<PullablePlatform>();
                if (pullablePlatform == null) pullablePlatform = instance.AddComponent<PullablePlatform>();
                pullablePlatform.Initialize(data.rule.pullLimit);
            }

            spawnedList.Add(instance);
        }
    }

    protected abstract int CarveTerrain(int startY);
    protected abstract Vector2Int PlacePlatforms(Vector2Int startPlatform);
}