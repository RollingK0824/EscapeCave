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

    [Header("해금 조건 (null일 경우 항상 스폰 가능)")]
    public UnlockNodeData requiredUnlockNode;

    public bool IsUnlocked()
    {
        if (requiredUnlockNode == null) return true;

        if (Managers.DataManager.Instance != null)
        {
            return Managers.DataManager.Instance.IsUnlocked(requiredUnlockNode);
        }

        return true;
    }
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

    [Header("일반 오브젝트/상자/골드 스폰 설정 (해금 조건 지원)")]
    public List<SpawnRule> groundObjectSpawns;
    public List<SpawnRule> platformObjectSpawns;
    public List<SpawnRule> ceilingObjectSpawns;
    public List<SpawnRule> underPlatformObjectSpawns;
    [Tooltip("오브젝트 간 최소 X축 스폰 간격 (타일 수)")]
    public int minObjectSpawnGapX = 4;

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

    // Virtual properties for platform constraints (can be overridden by subclasses)
    public virtual int MinJumpDistance => 4;
    public virtual int MaxJumpDistance => 8;
    public virtual int MinPlatformHeight => 5;
    public virtual int MaxPlatformHeight => chunkHeight - 5;

    protected abstract void InitializeTerrainBackground();
    protected abstract void ApplyThemeSpecificTerrain();

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

        // 1. Generate path and platforms first
        endPlatform = GeneratePathAndPlatforms(genParams.startPlatform);

        // 2. Initialize background terrain
        InitializeTerrainBackground();

        // 3. Carve clearance around the path
        CarveClearanceAroundPath();

        // 4. Apply theme-specific terrain details
        ApplyThemeSpecificTerrain();

        // 5. Connect boundaries and write platforms to mapData
        WritePlatformsToMapData();
        SmoothBoundaries(genParams.startY, endPlatform.y);
        ForceTransitionTunnel(genParams.startY);

        // 6. Draw to tilemap and spawn objects
        RenderToTilemap();
        SpawnObjects();

        return endPlatform.y;
    }

    protected virtual Vector2Int GeneratePathAndPlatforms(Vector2Int startPlatform)
    {
        Vector2Int lastPlatformEnd = startPlatform;
        int currentX = Mathf.Max(0, lastPlatformEnd.x);
        int currentY = lastPlatformEnd.y;

        int minY = MinPlatformHeight;
        int maxY = MaxPlatformHeight;
        currentY = Mathf.Clamp(currentY, minY, maxY);

        // 첫 번째 플랫폼 추가 (이전 청크의 끝 플랫폼이 유효하지 않은 경우 보정)
        if (pendingPlatforms.Count == 0 && currentX <= 0)
        {
            if (platformConfigurations != null && platformConfigurations.Count > 0)
            {
                PlatformSpawnRule rule = platformConfigurations[0];
                int chosenLength = chunkRandom.Next(rule.minLength, rule.maxLength + 1);
                if (chosenLength < 1) chosenLength = 1;
                pendingPlatforms.Add(new PlatformSpawnData
                {
                    localX = 0,
                    localY = currentY,
                    chosenLength = chosenLength,
                    rule = rule
                });
                currentX = chosenLength - 1;
            }
        }

        while (currentX < chunkWidth - 15)
        {
            int jumpX = chunkRandom.Next(MinJumpDistance, MaxJumpDistance + 1);
            int jumpY = chunkRandom.Next(-3, 4); // Y 고도차 -3 ~ +3
            int nextY = currentY + jumpY;
            nextY = Mathf.Clamp(nextY, minY, maxY);

            if (platformConfigurations == null || platformConfigurations.Count == 0)
            {
                currentX += jumpX;
                currentY = nextY;
                continue;
            }

            PlatformSpawnRule rule = platformConfigurations[chunkRandom.Next(0, platformConfigurations.Count)];
            if (chunkRandom.NextDouble() > rule.spawnChance)
            {
                rule = platformConfigurations[0]; // fallback
            }

            int chosenLength = chunkRandom.Next(rule.minLength, rule.maxLength + 1);
            if (chosenLength < 1) chosenLength = 1;

            int nextStartX = currentX + jumpX;

            pendingPlatforms.Add(new PlatformSpawnData
            {
                localX = nextStartX,
                localY = nextY,
                chosenLength = chosenLength,
                rule = rule
            });

            currentX = nextStartX + chosenLength - 1;
            currentY = nextY;

            mainPath.Add(new Vector2Int(nextStartX + chosenLength / 2, nextY));
        }

        if (pendingPlatforms.Count > 0)
        {
            var lastPlat = pendingPlatforms[pendingPlatforms.Count - 1];
            return new Vector2Int(lastPlat.localX + lastPlat.chosenLength - 1, lastPlat.localY);
        }
        return new Vector2Int(chunkWidth - 1, currentY);
    }

    protected virtual void CarveClearanceAroundPath()
    {
        int headroom = 4;
        foreach (var platform in pendingPlatforms)
        {
            int startX = platform.localX;
            int endX = platform.localX + platform.chosenLength;

            if (platform.rule.type == PlatformType.Moving)
            {
                if (platform.rule.moveDirection == MoveDirection.Horizontal)
                {
                    endX += platform.rule.moveRange;
                }
            }
            else if (platform.rule.type == PlatformType.Pullable)
            {
                endX += Mathf.CeilToInt(platform.rule.pullLimit);
            }

            for (int x = startX; x < endX; x++)
            {
                for (int y = platform.localY + 1; y <= platform.localY + headroom; y++)
                {
                    if (x >= 0 && x < chunkWidth && y >= 0 && y < chunkHeight)
                    {
                        mapData[x, y] = 0;
                    }
                }
            }
        }

        for (int i = 0; i < pendingPlatforms.Count - 1; i++)
        {
            var p1 = pendingPlatforms[i];
            var p2 = pendingPlatforms[i + 1];

            int p1EndX = p1.localX + p1.chosenLength - 1;
            if (p1.rule.type == PlatformType.Moving && p1.rule.moveDirection == MoveDirection.Horizontal)
            {
                p1EndX += p1.rule.moveRange;
            }
            else if (p1.rule.type == PlatformType.Pullable)
            {
                p1EndX += Mathf.CeilToInt(p1.rule.pullLimit);
            }

            Vector2Int startPt = new Vector2Int(p1EndX, p1.localY + 1);
            Vector2Int endPt = new Vector2Int(p2.localX, p2.localY + 1);

            CarveCapsule(startPt, endPt, 3);
        }
    }

    protected void CarveCapsule(Vector2Int pA, Vector2Int pB, int radius)
    {
        int minX = Mathf.Min(pA.x, pB.x) - radius;
        int maxX = Mathf.Max(pA.x, pB.x) + radius;
        int minY = Mathf.Min(pA.y, pB.y) - radius;
        int maxY = Mathf.Max(pA.y, pB.y) + radius;

        Vector2 a = pA;
        Vector2 b = pB;
        Vector2 ab = b - a;
        float l2 = ab.sqrMagnitude;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (x >= 0 && x < chunkWidth && y >= 0 && y < chunkHeight)
                {
                    Vector2 p = new Vector2(x, y);
                    float t = 0;
                    if (l2 > 0)
                    {
                        t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
                    }
                    Vector2 projection = a + t * ab;
                    if (Vector2.Distance(p, projection) <= (float)radius)
                    {
                        mapData[x, y] = 0;
                    }
                }
            }
        }
    }

    protected void WritePlatformsToMapData()
    {
        foreach (var data in pendingPlatforms)
        {
            int markID = 2;
            int requiredLength = data.chosenLength;
            int checkMaxX = data.localX + requiredLength;
            int checkMaxY = data.localY + 1;

            if (data.rule.type == PlatformType.Moving)
            {
                markID = 4;
                if (data.rule.moveDirection == MoveDirection.Horizontal) checkMaxX += data.rule.moveRange;
                else checkMaxY += data.rule.moveRange;
            }
            else if (data.rule.type == PlatformType.Pullable)
            {
                markID = 5;
                checkMaxX += Mathf.CeilToInt(data.rule.pullLimit);
            }

            for (int x = data.localX; x < checkMaxX; x++)
            {
                for (int y = data.localY; y < checkMaxY; y++)
                {
                    if (x >= 0 && x < chunkWidth && y >= 0 && y < chunkHeight)
                    {
                        mapData[x, y] = markID;
                    }
                }
            }
        }
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

        // 플랫폼을 먼저 생성해야 그 위에 스폰되는 오브젝트들이 정상적으로 바닥을 인식할 수 있습니다.
        foreach (var data in pendingPlatforms)
        {
            SpawnPlatform(data);
        }

        int lastGroundSpawnX = -minSpawnGapX;
        int lastCeilingSpawnX = -minSpawnGapX;
        int lastUnderPlatformSpawnX = -minSpawnGapX;
        int lastPlatformSpawnX = -minSpawnGapX;

        int lastGroundObjX = -minObjectSpawnGapX;
        int lastCeilingObjX = -minObjectSpawnGapX;
        int lastUnderPlatformObjX = -minObjectSpawnGapX;
        int lastPlatformObjX = -minObjectSpawnGapX;

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
                    if (x - lastGroundObjX >= minObjectSpawnGapX)
                    {
                        if (TrySpawnObject(groundObjectSpawns, x, y + 1)) lastGroundObjX = x;
                    }
                }

                // 천장 검사
                if (y > 0 && mapData[x, y] == 1 && mapData[x, y - 1] == 0)
                {
                    if (x - lastCeilingSpawnX >= minSpawnGapX)
                    {
                        if (TrySpawnObject(ceilingSpawns, x, y - 1)) lastCeilingSpawnX = x;
                    }
                    if (x - lastCeilingObjX >= minObjectSpawnGapX)
                    {
                        if (TrySpawnObject(ceilingObjectSpawns, x, y - 1)) lastCeilingObjX = x;
                    }
                }

                // 플랫폼 밑 검사
                if (y > 0 && (mapData[x, y] == 2 || mapData[x, y] == 4 || mapData[x, y] == 5) && mapData[x, y - 1] == 0)
                {
                    if (x - lastUnderPlatformSpawnX >= minSpawnGapX)
                    {
                        if (TrySpawnObject(underPlatformSpawns, x, y - 1)) lastUnderPlatformSpawnX = x;
                    }
                    if (x - lastUnderPlatformObjX >= minObjectSpawnGapX)
                    {
                        if (TrySpawnObject(underPlatformObjectSpawns, x, y - 1)) lastUnderPlatformObjX = x;
                    }
                }

                // 플랫폼 위 검사
                if (y < chunkHeight - 1 && (mapData[x, y] == 2 || mapData[x, y] == 4 || mapData[x, y] == 5) && mapData[x, y + 1] == 0)
                {
                    if (x - lastPlatformSpawnX >= minSpawnGapX)
                    {
                        if (TrySpawnObject(platformSpawns, x, y + 1)) lastPlatformSpawnX = x;
                    }
                    if (x - lastPlatformObjX >= minObjectSpawnGapX)
                    {
                        if (TrySpawnObject(platformObjectSpawns, x, y + 1)) lastPlatformObjX = x;
                    }
                }
            }
        }
        SpawnThemeSpecificObjects();
    }

    protected virtual void SpawnThemeSpecificObjects()
    {
    }

    protected bool TrySpawnObject(List<SpawnRule> rules, int localX, int localY)
    {
        if (rules == null || rules.Count == 0) return false;

        List<SpawnRule> validRules = new List<SpawnRule>();
        for (int i = 0; i < rules.Count; i++)
        {
            if (rules[i].IsUnlocked())
            {
                validRules.Add(rules[i]);
            }
        }

        if (validRules.Count == 0) return false;

        int randomIdx = chunkRandom.Next(0, validRules.Count);
        SpawnRule rule = validRules[randomIdx];

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

            // 3. 양끝 투명 콜라이더(LeftLedge, RightLedge) 위치 보정
            Transform leftLedge = instance.transform.Find("LeftLedge");
            Transform rightLedge = instance.transform.Find("RightLedge");
            if (leftLedge != null || rightLedge != null)
            {
                bool isScaled = Mathf.Approximately(instance.transform.localScale.x, data.chosenLength);
                float offsetValue = isScaled ? 0.5f : (data.chosenLength / 2f);

                if (leftLedge != null)
                {
                    leftLedge.localPosition = new Vector3(-offsetValue, leftLedge.localPosition.y, 0f);
                    if (isScaled)
                    {
                        // 부모의 Scale.x 확장에 따라 자식의 가로 폭이 비정상적으로 늘어나는 것을 방지 (역수 곱하기)
                        leftLedge.localScale = new Vector3(1f / data.chosenLength, leftLedge.localScale.y, leftLedge.localScale.z);
                    }
                }
                if (rightLedge != null)
                {
                    rightLedge.localPosition = new Vector3(offsetValue, rightLedge.localPosition.y, 0f);
                    if (isScaled)
                    {
                        rightLedge.localScale = new Vector3(1f / data.chosenLength, rightLedge.localScale.z, rightLedge.localScale.z);
                    }
                }
            }

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

}