using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator_Ver_PerlinNoise : MonoBehaviour
{
    [Header("타일맵 렌더러")]
    public Tilemap[] tilemap;

    [Header("데이터 에셋 (Scriptable Object)")]
    public TileDataSO tileSetData;
    public StageManifestSO stageManifest;

    [Header("플랫폼 도약 보정치")]
    public int maxJumpDistanceX = 4;
    public int maxJumpDistanceY = 3;

    private int[,] _mapData;
    private int[] _surfaceHeights;
    private List<Vector2Int> _mainPath;

    private int _currentWidth;
    private int _currentHeight;

    private int _currentTileIdx = 0;

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

        tilemap[_currentTileIdx].ClearAllTiles();
        ClearObjects();

        // Pass 0: 기본 외곽벽(ID 1) 채우기
        InitializeMapWithWalls();

        Pass1_CarveExtremeTunnel();
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

    private void Pass0_GeneratePerlinBottom()
    {
        _surfaceHeights = new int[_currentWidth]; // 높이 지도 배열 초기화
        float seedOffset = Random.Range(0f, 10000f);

        // 맵 높이(50)에 맞는 극단적인 산맥 수치 (원하시면 인스펙터로 빼셔도 됩니다)
        float noiseScale = 0.03f; // 0.1보다 작아야 산맥이 크고 장엄하게 그려집니다.
        int maxBottomHeight = 35; // 높이 50 중 35까지 치솟는 거대한 산맥
        int minBottomHeight = 5;  // 제일 깊은 골짜기

        for (int x = 0; x < _currentWidth; x++)
        {
            float rawNoise = Mathf.PerlinNoise((x * noiseScale) + seedOffset, 0f);
            int targetHeight = minBottomHeight + Mathf.FloorToInt(rawNoise * (maxBottomHeight - minBottomHeight));

            // 완성된 산맥의 Y높이를 배열에 저장해 Pass1에게 공유
            _surfaceHeights[x] = targetHeight;

            for (int y = 0; y < targetHeight; y++)
            {
                if (IsValidCoordinate(x, y))
                {
                    _mapData[x, y] = 1; // 1번: 산맥(벽)
                }
            }
        }
    }
    private void Pass1_CarveExtremeTunnel()
    {
        int radius = stageManifest.tunnelRadius;

        // 터널의 시작점 (Y축을 랜덤으로 시작)
        Vector2Int currentPos = new Vector2Int(10, Random.Range(15, _currentHeight - 15));

        while (currentPos.x < _currentWidth - 15)
        {
            // [핵심 1] 다음 목표 지점(Waypoint) 설정
            // X는 20~40칸 앞으로 전진
            int nextX = Mathf.Min(currentPos.x + Random.Range(20, 40), _currentWidth - 15);

            // Y는 이전 위치를 깡그리 무시하고 맵 최하단~최상단 사이에서 무작위로 찍어버림!
            // 이 수치가 극단적으로 차이 날수록 앞을 가로막는 '수직 절벽'이 생성됩니다.
            int nextY = Random.Range(radius + 5, _currentHeight - radius - 5);

            Vector2Int nextWaypoint = new Vector2Int(nextX, nextY);

            // [핵심 2] 현재 위치에서 다음 목표 지점까지 미친듯이 땅을 파며 이동
            while (currentPos != nextWaypoint)
            {
                if (!_mainPath.Contains(currentPos)) _mainPath.Add(currentPos);
                CarveCircle(currentPos, radius);

                // 이동 로직: 목표를 향해 X 또는 Y축으로 1칸씩 갉아먹으며 이동
                if (currentPos.x < nextWaypoint.x && currentPos.y != nextWaypoint.y)
                {
                    // 대각선/지그재그 느낌을 살리기 위해 50% 확률로 X 이동, 50% 확률로 Y 이동
                    if (Random.value < 0.5f)
                    {
                        currentPos.x++;
                    }
                    else
                    {
                        currentPos.y += (nextWaypoint.y > currentPos.y) ? 1 : -1;
                    }
                }
                else if (currentPos.x < nextWaypoint.x)
                {
                    currentPos.x++; // 목표 높이에 도달했으면 앞으로 직진
                }
                else if (currentPos.y != nextWaypoint.y)
                {
                    currentPos.y += (nextWaypoint.y > currentPos.y) ? 1 : -1; // X가 같으면 수직으로 파고 올라감/내려감
                }
            }
        }
    }

    //// [완전 개편] Pass 1: 산맥의 굴곡을 정확히 따라가며 터널 파기
    //private void Pass1_CarveTunnel()
    //{
    //    int radius = stageManifest.tunnelRadius;

    //    // 산맥 표면에서 터널 중심축이 얼마나 떨어져 있을지 결정 (수치가 작을수록 바닥과 밀착됨)
    //    int paddingY = radius - 1;

    //    // X축을 1칸씩 전진하며 롤러코스터처럼 산맥을 따라갑니다.
    //    for (int x = 10; x < _currentWidth - 15; x++)
    //    {
    //        // Pass0에서 만든 산맥의 높이를 읽어옴
    //        int surfaceY = _surfaceHeights[x];

    //        // 터널의 Y 위치를 산맥 표면 바로 위로 설정 (산맥을 관통하지 않고 타고 넘게 됨)
    //        int centerY = surfaceY + paddingY;

    //        // 천장 뚫림 방지용 안전 클램프
    //        centerY = Mathf.Clamp(centerY, radius + 2, _currentHeight - radius - 2);

    //        Vector2Int current = new Vector2Int(x, centerY);

    //        if (!_mainPath.Contains(current))
    //        {
    //            _mainPath.Add(current);
    //        }
    //        CarveCircle(current, radius);
    //    }
    //}

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
                    tilemap[_currentTileIdx].SetTile(new Vector3Int(x, y, 0), tileSetData.tiles[tileID]);
                }
                else
                {
                    tilemap[_currentTileIdx].SetTile(new Vector3Int(x, y, 0), null);
                }
            }
        }
    }

    private void ClearObjects()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (child != tilemap[_currentTileIdx].gameObject) DestroyImmediate(child);
        }
    }
}