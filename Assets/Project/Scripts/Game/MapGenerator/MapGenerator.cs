using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    [Header("단일 글로벌 타일맵 및 추적")]
    public Transform player;
    public Tilemap globalTilemap;

    [Header("스테이지 테마 리스트")]
    public BaseMapRuleSO[] stageRules;

    [Header("시드 시스템")]
    public float masterSeed;
    public bool useRandomSeedAtStart = true;

    private int[] _chunkOffsets = new int[3];
    private int _currentChunkIdx = 0;
    private int _lastExitY;
    private int _chunkWidth;
    private int _chunkGenerationCount = 0;

    private int[,] _mapDataBuffer;
    private TileBase[] _clearBuffer;
    private Vector2Int _lastPlatformLocal;

    private void Start()
    {
        if (stageRules.Length == 0) return;
        if (useRandomSeedAtStart) masterSeed = Random.Range(0f, 50000f);

        BaseMapRuleSO initialRule = stageRules[0];
        _chunkWidth = initialRule.chunkWidth;
        _lastExitY = initialRule.chunkHeight / 2;
        _lastPlatformLocal = new Vector2Int(0, _lastExitY);

        _mapDataBuffer = new int[_chunkWidth, initialRule.chunkHeight];
        _clearBuffer = new TileBase[_chunkWidth * initialRule.chunkHeight];

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

            _lastExitY = randomRule.GenerateChunk(
                globalTilemap, _mapDataBuffer, _chunkOffsets[i], _lastExitY, chunkSeed, _lastPlatformLocal, out Vector2Int newPlatformEnd);

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
    }

    private void ShiftChunks()
    {
        int pastIdx = _currentChunkIdx;
        int futureIdx = (_currentChunkIdx + 1) % 3;

        BoundsInt clearBounds = new BoundsInt(_chunkOffsets[pastIdx], 0, 0, _chunkWidth, stageRules[0].chunkHeight, 1);
        globalTilemap.SetTilesBlock(clearBounds, _clearBuffer);

        float maxPosX = Mathf.Max(_chunkOffsets[0], _chunkOffsets[1], _chunkOffsets[2]);
        int newOffsetX = Mathf.RoundToInt(maxPosX + _chunkWidth);
        _chunkOffsets[pastIdx] = newOffsetX;

        int randomIdx = Random.Range(0, stageRules.Length);
        BaseMapRuleSO currentRule = stageRules[randomIdx];
        float chunkSeed = masterSeed + _chunkGenerationCount++;

        _lastExitY = currentRule.GenerateChunk(
            globalTilemap, _mapDataBuffer, newOffsetX, _lastExitY, chunkSeed, _lastPlatformLocal, out Vector2Int newPlatformEnd);

        _lastPlatformLocal = new Vector2Int(newPlatformEnd.x - _chunkWidth, newPlatformEnd.y);
        _currentChunkIdx = futureIdx;
    }
}