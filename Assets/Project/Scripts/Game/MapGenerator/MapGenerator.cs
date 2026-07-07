using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("추적 및 렌더링 설정")]
    public Transform player;
    public MapChunk chunkPrefab;

    [Header("스테이지 테마 리스트")]
    public BaseMapRuleSO[] stageRules;
    private int _currentStageIndex = 0;

    [Header("시드 시스템 통제")]
    public float masterSeed; // 인스펙터에서 고정 시드를 입력해 테스트할 수도 있음
    public bool useRandomSeedAtStart = true;

    private MapChunk[] _chunks = new MapChunk[3];
    private int _currentChunkIdx = 0;
    private int _lastExitY;
    private float _chunkWidth;

    // 몇 번째 청크를 생성 중인지 카운트하여 각 청크마다 고유한 난수 시드 제공
    private int _chunkGenerationCount = 0;

    private void Start()
    {
        if (stageRules.Length == 0) return;

        // 첫 번째 룰을 기준으로 청크 가로 길이 캐싱
        _chunkWidth = stageRules[_currentStageIndex].chunkWidth;

        // 임의의 중간 지점 높이에서 최초 시작
        _lastExitY = stageRules[_currentStageIndex].chunkHeight / 2;

        int initialStartY = stageRules[_currentStageIndex].chunkHeight / 2;
        _lastExitY = initialStartY;

        InitializeChunks();

        if (player != null)
        {
            if (player.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
            {
                rb.linearVelocity = Vector2.zero;
                rb.position = new Vector2(2f, initialStartY);
            }
            else
            {
                player.position = new Vector3(2f, initialStartY, player.position.z);
            }
        }
    }

    private void InitializeChunks()
    {
        for (int i = 0; i < 3; i++)
        {
            _chunks[i] = Instantiate(chunkPrefab, transform);

            float posX = i * _chunkWidth;
            _chunks[i].transform.position = new Vector3(posX, 0, 0);

            float chunkSeed = masterSeed + _chunkGenerationCount;
            ++_chunkGenerationCount;

            _lastExitY = _chunks[i].BuildMap(stageRules[_currentStageIndex], _lastExitY, chunkSeed);
        }
    }

    private void Update()
    {
        float shiftThreshold = _chunks[_currentChunkIdx].transform.position.x + (_chunkWidth * 1.5f);

        if (player.position.x > shiftThreshold)
        {
            ShiftChunks();
        }
    }

    private void ShiftChunks()
    {
        int pastIdx = _currentChunkIdx;
        int futureIdx = (_currentChunkIdx + 1) % 3;

        float maxPosX = Mathf.Max(
            _chunks[0].transform.position.x,
            _chunks[1].transform.position.x,
            _chunks[2].transform.position.x
        );

        float newPosX = maxPosX + _chunkWidth;
        _chunks[pastIdx].transform.position = new Vector3(newPosX, 0, 0);

        BaseMapRuleSO currentRule = stageRules[_currentStageIndex];
        float chunkSeed = masterSeed + _chunkGenerationCount;
        _chunkGenerationCount++;

        _lastExitY = _chunks[pastIdx].BuildMap(currentRule, _lastExitY, chunkSeed);

        _currentChunkIdx = futureIdx;
    }
}