using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    [Header("타일맵 및 에셋")]
    public Tilemap tilemap;
    public TileBase wallRuleTile;
    public TileBase platformRuleTile;

    [Header("프리팹")]
    public GameObject keyPrefab;
    public GameObject doorPrefab;
    public GameObject hideoutPrefab;
    public GameObject chaseObstaclePrefab;

    [Header("맵 크기")]
    public int width = 150;
    public int height = 80;
    public int tunnelRadius = 6;

    [Header("플랫폼 도약 보정")]
    public int maxJumpDistanceX = 4; // 플레이어가 넘을 수 있는 최대 가로 점프
    public int maxJumpDistanceY = 3; // 플레이어가 뛸 수 있는 최대 세로 점프

    [Header("기믹 배치 확률")]
    [Range(0f, 1f)] public float eventSpawnChance = 0.05f; // 매 칸마다 이벤트가 스폰될 확률 (5%)
    public int minDistanceBetweenEvents = 15; // 이벤트 간 최소 간격

    private int[,] _mapData; // 0: 허공, 1: 벽, 2: 발판
    private List<Vector2Int> _mainPath;

    // 이벤트 배치 상태 머신용 Enum
    private enum EventState { NeedKey, NeedDoor, NeedHideout, Done }

    void Start()
    {
        GenerateStage();
    }

    [ContextMenu("Generate Stage")]
    public void GenerateStage()
    {
        _mapData = new int[width, height];
        _mainPath = new List<Vector2Int>();
        tilemap.ClearAllTiles();
        ClearObjects();

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                _mapData[x, y] = 1;

        Pass1_CarveTunnel();
        Pass2_AnchorPlatforms(); // 펄린 노이즈 대신 궤적 기반 발판 보정
        Pass3_PlaceEventsDynamically(); // 순회형 무작위 배치
        Pass4_RenderToTilemap();
    }

    private void Pass1_CarveTunnel()
    {
        Vector2Int current = new Vector2Int(10, 10);
        Vector2Int end = new Vector2Int(width - 15, height - 15);

        while (current.x < end.x || current.y < end.y)
        {
            if (!_mainPath.Contains(current)) _mainPath.Add(current);
            CarveCircle(current, tunnelRadius);

            // [수정된 부분] X, Y 모두 목표에 도달하지 않았을 때만 무작위 이동
            if (current.x < end.x && current.y < end.y)
            {
                int dir = Random.Range(0, 4);
                if (dir == 0) current.x++;
                else if (dir == 1) current.y++;
                else if (dir == 2) { current.x++; current.y++; }
                else if (dir == 3 && current.y > 5) { current.x++; current.y--; }
                else { current.x++; current.y++; }
            }
            // Y는 목표에 도달했고, X만 남았을 경우 강제 우측 이동
            else if (current.x < end.x)
            {
                current.x++;
            }
            // X는 목표에 도달했고, Y만 남았을 경우 강제 상단 이동
            else if (current.y < end.y)
            {
                current.y++;
            }
        }
    }

    private void CarveCircle(Vector2Int center, int radius)
    {
        for (int x = -radius; x <= radius; x++)
            for (int y = -radius; y <= radius; y++)
                if (x * x + y * y <= radius * radius)
                {
                    int nx = center.x + x;
                    int ny = center.y + y;
                    if (nx > 2 && nx < width - 2 && ny > 2 && ny < height - 2)
                        _mapData[nx, ny] = 0;
                }
    }

    // [핵심 변경 1] 궤적을 따라가며 절대 못 넘어가는 구간이 없도록 발판을 박아넣음
    private void Pass2_AnchorPlatforms()
    {
        Vector2Int lastPlatformPos = _mainPath[0];

        foreach (Vector2Int pathNode in _mainPath)
        {
            // 마지막 발판과 현재 궤적의 거리가 플레이어의 최대 점프력을 벗어나려 할 때
            if (Mathf.Abs(pathNode.x - lastPlatformPos.x) >= maxJumpDistanceX ||
                Mathf.Abs(pathNode.y - lastPlatformPos.y) >= maxJumpDistanceY)
            {
                // 현재 궤적보다 약간 아래(발밑)에 2~3칸짜리 발판을 강제 생성
                int platformY = pathNode.y - 2;
                if (platformY > 2)
                {
                    _mapData[pathNode.x, platformY] = 2;
                    _mapData[pathNode.x + 1, platformY] = 2; // 발판 길이 확보

                    lastPlatformPos = new Vector2Int(pathNode.x, platformY);
                }
            }
        }
    }

    // [핵심 변경 2] 상태 머신을 활용한 순회형 기믹 배치
    private void Pass3_PlaceEventsDynamically()
    {
        EventState currentState = EventState.NeedKey;
        int distanceSinceLastEvent = minDistanceBetweenEvents; // 시작하자마자 나올 수 있도록 초기화

        for (int i = 0; i < _mainPath.Count; i++)
        {
            distanceSinceLastEvent++;

            // 이벤트 간 최소 거리를 만족하지 않았거나, 모든 배치가 끝났다면 스킵
            if (distanceSinceLastEvent < minDistanceBetweenEvents || currentState == EventState.Done)
                continue;

            // 너무 늦게 스폰되는 것을 막기 위해 뒤로 갈수록 확률을 강제로 높임
            float dynamicChance = eventSpawnChance + (float)i / _mainPath.Count * 0.1f;

            if (Random.value < dynamicChance)
            {
                Vector2Int spawnNode = _mainPath[i];

                if (currentState == EventState.NeedKey)
                {
                    PlaceObjectAtFloor(spawnNode, keyPrefab);
                    currentState = EventState.NeedDoor;
                    distanceSinceLastEvent = 0;
                }
                else if (currentState == EventState.NeedDoor)
                {
                    PlaceObjectAtFloor(spawnNode, doorPrefab);
                    currentState = EventState.NeedHideout;
                    distanceSinceLastEvent = 0;
                }
                else if (currentState == EventState.NeedHideout)
                {
                    Vector3 hideoutPos = PlaceObjectAtFloor(spawnNode, hideoutPrefab);
                    if (hideoutPos != Vector3.zero)
                    {
                        Vector3 obstaclePos = hideoutPos + new Vector3(3, 0, 0);
                        Instantiate(chaseObstaclePrefab, obstaclePos, Quaternion.identity, transform);
                    }
                    currentState = EventState.Done; // 모든 이벤트 배치 완료
                }
            }
        }
    }

    private Vector3 PlaceObjectAtFloor(Vector2Int pathPoint, GameObject prefab)
    {
        int checkX = pathPoint.x;

        // [수정된 부분] 맵 경계선(좌우 끝단)을 벗어나는 좌표는 배치를 무시하는 안전 장치
        if (checkX < 1 || checkX >= width - 1) return Vector3.zero;

        for (int y = pathPoint.y; y > 2; y--)
        {
            // [수정된 부분] 천장(상단 끝단)을 벗어나는 오버플로우 방지
            if (y + 2 >= height) continue;

            if (_mapData[checkX, y] == 2 || _mapData[checkX, y] == 1)
            {
                // 바닥 평탄화
                _mapData[checkX - 1, y] = 2;
                _mapData[checkX, y] = 2;
                _mapData[checkX + 1, y] = 2;

                // 위쪽 공간 확보
                _mapData[checkX, y + 1] = 0;
                _mapData[checkX, y + 2] = 0;

                Vector3 spawnPos = new Vector3(checkX + 0.5f, y + 1f, 0f);
                if (prefab != null) Instantiate(prefab, spawnPos, Quaternion.identity, transform);

                return spawnPos;
            }
        }
        return Vector3.zero;
    }

    private void Pass4_RenderToTilemap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (_mapData[x, y] == 1) tilemap.SetTile(new Vector3Int(x, y, 0), wallRuleTile);
                else if (_mapData[x, y] == 2) tilemap.SetTile(new Vector3Int(x, y, 0), platformRuleTile);
            }
        }
    }

    private void ClearObjects()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (child != tilemap.gameObject) DestroyImmediate(child);
        }
    }
}