using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    [Header("단일 글로벌 타일맵 및 추적")]
    public Transform player;
    public Tilemap globalTilemap;
    public Tilemap waterTilemap;

    [Header("스테이지 테마 리스트")]
    public BaseMapRuleSO[] stageRules;

    [Header("시드 시스템")]
    public float masterSeed;
    public bool useRandomSeedAtStart = true;

    [Header("오브젝트 컬링 최적화 설정")]
    [Tooltip("플레이어와 이 X축 거리(타일) 이상 멀어지면 몬스터/오브젝트 비활성화")]
    public float cullingDistance = 45f;

    private int[] _chunkOffsets = new int[3];
    private int _currentChunkIdx = 0;
    private int _lastExitY;
    private int _chunkWidth;
    private int _chunkGenerationCount = 0;

    private int[,] _mapDataBuffer;
    private TileBase[] _clearBuffer;
    private Vector2Int _lastPlatformLocal;

    private List<GameObject>[] _spawnedObjectsPerChunk = new List<GameObject>[3];

    private void Start()
    {
        if (stageRules.Length == 0) return;

        if (useRandomSeedAtStart)
        {
            byte[] seedBytes = new byte[4];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(seedBytes);
            }
            int rawSeed = System.BitConverter.ToInt32(seedBytes, 0);
            
            masterSeed = Mathf.Abs(rawSeed) % 1000000;
        }

        for (int i = 0; i < 3; i++)
        {
            _spawnedObjectsPerChunk[i] = new List<GameObject>();
        }

        BaseMapRuleSO initialRule = stageRules[0];
        _chunkWidth = initialRule.chunkWidth;
        _lastExitY = initialRule.chunkHeight / 2;
        _lastPlatformLocal = new Vector2Int(0, _lastExitY);

        _mapDataBuffer = new int[_chunkWidth, initialRule.chunkHeight];
        _clearBuffer = new TileBase[_chunkWidth * initialRule.chunkHeight];

        /* 임시 테스트용 플레이어 위치 변경 로직 */
        player.position = new Vector3(0, _lastExitY, 0);

        InitializeGlobalMap();
    }

    private void InitializeGlobalMap()
    {
        for (int i = 0; i < 3; i++)
        {
            _chunkOffsets[i] = i * _chunkWidth;
            int randomIdx = Random.Range(0, stageRules.Length);
            BaseMapRuleSO randomRule = stageRules[randomIdx];
            float chunkSeed = masterSeed + _chunkGenerationCount++;

            ChunkGenParams genParams = new ChunkGenParams
            {
                globalTilemap = this.globalTilemap,
                waterTilemap = this.waterTilemap,
                mapData = this._mapDataBuffer,
                offsetX = this._chunkOffsets[i],
                startY = this._lastExitY,
                seed = chunkSeed,
                startPlatform = this._lastPlatformLocal,
                spawnedList = this._spawnedObjectsPerChunk[i]
            };

            _lastExitY = randomRule.GenerateChunk(genParams, out Vector2Int newPlatformEnd);

            _lastPlatformLocal = new Vector2Int(newPlatformEnd.x - _chunkWidth, newPlatformEnd.y);
        }
    }

    private void Update()
    {
        float shiftThreshold = _chunkOffsets[_currentChunkIdx] + (_chunkWidth * 1.5f);

        if (player.position.x > shiftThreshold)
        {
            ShiftChunks();
        }

        UpdateObjectCulling();
    }

    private void UpdateObjectCulling()
    {
        if (player == null) return;

        float playerX = player.position.x;

        for (int i = 0; i < 3; i++)
        {
            List<GameObject> chunkObjects = _spawnedObjectsPerChunk[i];
            for (int j = 0; j < chunkObjects.Count; j++)
            {
                GameObject obj = chunkObjects[j];
                if (obj != null)
                {
                    float dist = Mathf.Abs(obj.transform.position.x - playerX);
                    bool shouldActive = dist <= cullingDistance;

                    if (obj.activeSelf != shouldActive)
                    {
                        obj.SetActive(shouldActive);
                    }
                }
            }
        }
    }

    private void ShiftChunks()
    {
        int pastIdx = _currentChunkIdx;
        int futureIdx = (_currentChunkIdx + 1) % 3;

        List<GameObject> oldObjects = _spawnedObjectsPerChunk[pastIdx];
        for (int i = 0; i < oldObjects.Count; i++)
        {
            if (oldObjects[i] != null)
            {
                var mapObj = oldObjects[i].GetComponent<MapSpawnedObject>();
                if (mapObj != null)
                {
                    Managers.PoolManager.Instance.Push(oldObjects[i], mapObj.poolKey);
                }
                else
                {
                    // 예외적으로 컴포넌트가 누락된 경우 일반 파괴
                    Destroy(oldObjects[i]);
                }
            }
        }
        oldObjects.Clear();

        BoundsInt clearBounds = new BoundsInt(_chunkOffsets[pastIdx], 0, 0, _chunkWidth, stageRules[0].chunkHeight, 1);
        globalTilemap.SetTilesBlock(clearBounds, _clearBuffer);
        if (waterTilemap != null)
        {
            waterTilemap.SetTilesBlock(clearBounds, _clearBuffer);
        }

        float maxPosX = Mathf.Max(_chunkOffsets[0], _chunkOffsets[1], _chunkOffsets[2]);
        int newOffsetX = Mathf.RoundToInt(maxPosX + _chunkWidth);
        _chunkOffsets[pastIdx] = newOffsetX;

        int randomIdx = Random.Range(0, stageRules.Length);
        BaseMapRuleSO currentRule = stageRules[randomIdx];
        float chunkSeed = masterSeed + _chunkGenerationCount++;

        ChunkGenParams genParams = new ChunkGenParams
        {
            globalTilemap = this.globalTilemap,
            waterTilemap = this.waterTilemap,
            mapData = this._mapDataBuffer,
            offsetX = newOffsetX,
            startY = this._lastExitY,
            seed = chunkSeed,
            startPlatform = this._lastPlatformLocal,
            spawnedList = oldObjects
        };

        _lastExitY = currentRule.GenerateChunk(genParams, out Vector2Int newPlatformEnd);

        _lastPlatformLocal = new Vector2Int(newPlatformEnd.x - _chunkWidth, newPlatformEnd.y);
        _currentChunkIdx = futureIdx;
    }
}