using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("추적 및 렌더링 설정")]
    public Transform player;
    public MapChunk chunkPrefab;

    [Header("스테이지 테마 리스트")]
    public BaseMapRuleSO[] stageRules; // 0번: 동굴/산맥, 1번: 지하호수 등
    private int _currentStageIndex = 0; // 현재 적용 중인 SO 인덱스

    // 풀링(Pooling) 관리를 위한 링 버퍼 변수들
    private MapChunk[] _chunks = new MapChunk[3];
    private int _currentChunkIdx = 0; // 현재 플레이어가 밟고 있는 청크의 배열 인덱스
    private int _lastExitY;           // 이전 청크가 뚫어놓은 터널의 끝점 높이

    private float _chunkWidth;

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

        if(player != null)
        {
            if(player.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
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

            // 위치 지정: 0번은 기준점, 1번은 미래(앞), 2번은 과거(뒤, 방어용)
            // 즉 [-width, 0, +width] 순서로 배치됩니다.
            float posX = i * _chunkWidth;
            _chunks[i].transform.position = new Vector3(posX, 0, 0);

            // 맵 생성 및 끝점 갱신 (2번 과거 청크는 시작하자마자 지워질 운명이므로 생성 생략해도 무방하나 통일성을 위해 생성)
            _lastExitY = _chunks[i].BuildMap(stageRules[_currentStageIndex], _lastExitY);
        }
    }

    private void Update()
    {
        // 최적화: 매 프레임 플레이어의 X 위치만 단순 비교 (물리 연산 X)
        // 플레이어가 '현재 청크'의 중간 지점(50%)을 넘어가면 다음 청크를 앞으로 당겨옵니다.
        float shiftThreshold = _chunks[_currentChunkIdx].transform.position.x + (_chunkWidth * 0.5f);

        if (player.position.x > shiftThreshold)
        {
            ShiftChunks();
        }
    }

    private void ShiftChunks()
    {
        // 링 버퍼 논리를 이용해 인덱스 계산 (나머지 연산자 활용)
        int pastIdx = (_currentChunkIdx + 2) % 3;   // 버려질 맨 뒤의 과거 청크
        int futureIdx = (_currentChunkIdx + 1) % 3; // 현재의 바로 앞 청크 (미래)

        float maxPosX = Mathf.Max(
            _chunks[0].transform.position.x,
            _chunks[1].transform.position.x,
            _chunks[2].transform.position.x
            );

        // 1. 과거 청크를 미래 청크의 바로 앞(다음 위치)으로 순간이동
        float newPosX = maxPosX + _chunkWidth;
        _chunks[pastIdx].transform.position = new Vector3(newPosX, 0, 0);

        // [확장성] 여기서 점수나 진행도에 따라 _currentStageIndex를 올려주면 
        // 다음 청크부터는 완전히 새로운 테마(SO)로 맵이 그려집니다!
        BaseMapRuleSO currentRule = stageRules[_currentStageIndex];

        // 2. 위치를 옮긴 청크에게 새로운 맵을 그리라고 지시 (데이터 덮어쓰기)
        _lastExitY = _chunks[pastIdx].BuildMap(currentRule, _lastExitY);

        // 3. 인덱스 업데이트 (미래 청크가 이제 '현재 청크'가 됨)
        _currentChunkIdx = futureIdx;
    }
}