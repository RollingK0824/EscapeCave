using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    [Header("타일맵 렌더러")]
    public Tilemap tilemap;

    [Header("데이터 에셋 (Scriptable Object)")]
    public TileDataSO tileSetData;
    public StageManifestSO stageManifest;

    [Header("플랫폼 도약 보정치")]
    public int maxJumpDistanceX = 4;
    public int maxJumpDistanceY = 3;

    private int[,] _mapData;
    private List<Vector2Int> _mainPath;

    private int _currentWidth;
    private int _currentHeight;

    void Start()
    {
        GenerateStage();
    }

    [ContextMenu("Generate Stage")]
    public void GenerateStage()
    {
        if (tilemap == null || tileSetData == null || stageManifest == null)
        {
            Debug.LogWarning("타일맵 또는 SO 데이터 에셋이 누락되었습니다.");
            return;
        }

        // SO에서 설정한 스테이지 크기 가져오기
        _currentWidth = stageManifest.stageWidth;
        _currentHeight = stageManifest.stageHeight;

        _mapData = new int[_currentWidth, _currentHeight];
        _mainPath = new List<Vector2Int>();

        tilemap.ClearAllTiles();
        ClearObjects();

        // Pass 0: 기본 외곽벽(ID 1) 채우기
        InitializeMapWithWalls();

        Pass1_CarveTunnel();
        Pass2_AnchorPlatforms();
        Pass3_PlaceEventsFlexibly(); // 세부 지형 제어가 포함된 이벤트 배치
        Pass4_RenderToTilemap();
    }

    private void InitializeMapWithWalls()
    {
        for (int x = 0; x < _currentWidth; x++)
        {
            for (int y = 0; y < _currentHeight; y++)
            {
                _mapData[x, y] = 1; // 1번: 벽
            }
        }
    }

    private bool IsValidCoordinate(int x, int y)
    {
        return x >= 0 && x < _currentWidth && y >= 0 && y < _currentHeight;
    }

    private void Pass1_CarveTunnel()
    {
        Vector2Int current = new Vector2Int(10, 10);
        Vector2Int end = new Vector2Int(_currentWidth - 15, _currentHeight - 15);
        int radius = stageManifest.tunnelRadius;

        while (current.x < end.x || current.y < end.y)
        {
            if (!_mainPath.Contains(current)) _mainPath.Add(current);

            CarveCircle(current, radius);

            int dir = Random.Range(0, 100);

            if (dir < 40)
            {
                // 40% 확률로 수평 직진
                current.x++;
            }
            else if (dir < 70)
            {
                // 30% 확률로 상향 대각선 이동 (단, 천장을 뚫지 않는 선에서)
                if (current.y < _currentHeight - radius - 5) current.y++;
                current.x++;
            }
            else
            {
                // 30% 확률로 하향 대각선 이동 (단, 바닥을 뚫지 않는 선에서)
                if (current.y > radius + 5) current.y--;
                current.x++;
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

                    if (nx > 2 && nx < _currentWidth - 2 && ny > 2 && ny < _currentHeight - 2)
                        _mapData[nx, ny] = 0;
                }
    }

    private void Pass2_AnchorPlatforms()
    {
        if (_mainPath.Count == 0) return;

        // 거리 측정의 기준을 '가장 최근에 생성된 발판의 끝부분'으로 잡습니다.
        Vector2Int lastPlatformEnd = _mainPath[0];

        foreach (Vector2Int pathNode in _mainPath)
        {
            int distX = Mathf.Abs(pathNode.x - lastPlatformEnd.x);
            int distY = Mathf.Abs(pathNode.y - lastPlatformEnd.y);

            // X나 Y 중 하나라도 점프 한계치에 다다랐을 때 생성 시도
            if (distX >= maxJumpDistanceX || distY >= maxJumpDistanceY)
            {
                int platY = pathNode.y - 2;
                int platformLength = Random.Range(3, 7); // 3~6칸의 넓직한 수평 발판

                // [핵심 해결책] 1. 발판 주변 안전거리(Clearance) 검사
                bool isClear = true;

                // 생성하려는 발판의 상하좌우+대각선 1칸씩을 모두 스캔합니다.
                for (int x = pathNode.x - 1; x <= pathNode.x + platformLength; x++)
                {
                    for (int y = platY - 1; y <= platY + 1; y++)
                    {
                        if (IsValidCoordinate(x, y))
                        {
                            // 주변에 이미 발판(2번)이 단 1칸이라도 있다면 무효 처리 (계단화 원천 차단)
                            if (_mapData[x, y] == 2)
                            {
                                isClear = false;
                                break;
                            }
                        }
                    }
                    if (!isClear) break;
                }

                // 주변이 완벽하게 비어있을 때만(isClear == true) 수평 발판 생성
                if (isClear)
                {
                    int actuallyPlaced = 0;
                    for (int i = 0; i < platformLength; i++)
                    {
                        int px = pathNode.x + i;

                        // 바깥 벽(1번)을 파먹지 않고 허공(0번)인 곳에만 일직선으로 배치
                        if (IsValidCoordinate(px, platY) && _mapData[px, platY] == 0)
                        {
                            _mapData[px, platY] = 2; // 2번: 발판 배치
                            actuallyPlaced++;
                        }
                    }

                    // 하나라도 정상 배치되었다면, 다음 거리 측정을 위해 기준점을 갱신
                    if (actuallyPlaced > 0)
                    {
                        // 새로 생성된 발판의 제일 '오른쪽 끝' 좌표로 갱신하여 겹침 방지
                        lastPlatformEnd = new Vector2Int(pathNode.x + actuallyPlaced - 1, platY);
                    }
                }
            }
        }
    }

    private void Pass3_PlaceEventsFlexibly()
    {
        var sequence = stageManifest.eventSequence;
        if (sequence == null || sequence.Count == 0) return;

        int currentEventIndex = 0;
        int distanceSinceLastEvent = 0;

        for (int i = 0; i < _mainPath.Count; i++)
        {
            distanceSinceLastEvent++;
            if (currentEventIndex >= sequence.Count) break;

            StageEvent currentEvent = sequence[currentEventIndex];
            if (distanceSinceLastEvent < currentEvent.minProgressDistance) continue;

            float finalChance = currentEvent.spawnChance + ((float)i / _mainPath.Count * 0.1f);

            if (Random.value < finalChance)
            {
                Vector2Int spawnNode = _mainPath[i];

                bool success = PlaceFlexibleObject(spawnNode, currentEvent);

                if (success)
                {
                    currentEventIndex++;
                    distanceSinceLastEvent = 0;
                }
            }
        }
    }

    private bool PlaceFlexibleObject(Vector2Int pathPoint, StageEvent ev)
    {
        int checkX = pathPoint.x;
        if (checkX < 1 || checkX >= _currentWidth - 1) return false;

        // 아래 방향 수직 스캔
        for (int y = pathPoint.y; y > 2; y--)
        {
            if (!IsValidCoordinate(checkX,y)) continue;

            // 땅을 발견했을 때 (ID 2: 발판 또는 ID 1: 벽)
            if (_mapData[checkX, y] == 2 || _mapData[checkX, y] == 1)
            {
                int originY = y + 1; // 오브젝트가 배치될 중심 Y 좌표

                if (ev.terrainModifiers != null)
                {
                    foreach (TileModifier mod in ev.terrainModifiers)
                    {
                        // 오프셋을 적용한 실질적 시작 타일 좌표 계산
                        int startX = checkX + mod.offset.x;
                        int startY = originY + mod.offset.y;

                        // 지정된 Size 만큼 타일 배열 변경 루프
                        for (int rx = 0; rx < mod.size.x; rx++)
                        {
                            for (int ry = 0; ry < mod.size.y; ry++)
                            {
                                int targetX = startX + rx;
                                int targetY = startY + ry;

                                // 맵 경계선을 벗어나지 않는 안전 검사 후 배열 덮어쓰기
                                if (IsValidCoordinate(targetX,targetY)&&tileSetData.IsValidTileID(mod.targetTileID))
                                {
                                    _mapData[targetX, targetY] = mod.targetTileID;
                                }
                            }
                        }
                    }
                }

                // 기믹 프리팹 월드 스폰
                Vector3 spawnPos = new Vector3(checkX + 0.5f, originY, 0f);
                if (ev.prefab != null)
                {
                    GameObject go = Instantiate(ev.prefab, spawnPos, Quaternion.identity, transform);
                    go.name = ev.eventName;
                }
                return true;
            }
        }
        return false;
    }

    private void Pass4_RenderToTilemap()
    {
        for (int x = 0; x < _currentWidth; x++)
        {
            for (int y = 0; y < _currentHeight; y++)
            {
                int tileID = _mapData[x, y];
                if (tileSetData.IsValidTileID(tileID) && tileSetData.tiles[tileID] != null)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), tileSetData.tiles[tileID]);
                }
                else
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), null);
                }
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