using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CaveRuleSO", menuName = "Scriptable Objects/CaveRuleSO")]
public class CaveRuleSO : BaseMapRuleSO
{
    [Header("동굴 고유 설정")]
    public int tunnelRadius = 6;
    public int minJumpDistance = 4;
    public int maxJumpDistance = 8;

    protected override int CarveTerrain(int[,] mapData, List<Vector2Int> mainPath, int totalWidth, int height, int startY, float seed)
    {
        // 일관된 난수를 위해 필요 시 유니티 Random 시드를 일시 고정할 수 있습니다.
        Random.InitState((int)seed);

        Vector2Int currentPos = new Vector2Int(0, startY);

        // [경계면 해결] 다음 청크 영역(totalWidth - 5)까지 뚫고 나가 끊김 현상을 방지
        while (currentPos.x < totalWidth - 5)
        {
            int nextX = Mathf.Min(currentPos.x + Random.Range(20, 40), totalWidth - 1);
            int nextY = Random.Range(tunnelRadius + 5, height - tunnelRadius - 5);
            Vector2Int nextWaypoint = new Vector2Int(nextX, nextY);

            while (currentPos != nextWaypoint)
            {
                // 플랫폼은 화면에 보이는 가시 너비 안에서만 노드 등록
                if (currentPos.x < chunkWidth && !mainPath.Contains(currentPos))
                {
                    mainPath.Add(currentPos);
                }

                CarveCircle(mapData, totalWidth, height, currentPos, tunnelRadius);

                if (currentPos.x < nextWaypoint.x && currentPos.y != nextWaypoint.y)
                {
                    if (Random.value < 0.5f) currentPos.x++;
                    else currentPos.y += (nextWaypoint.y > currentPos.y) ? 1 : -1;
                }
                else if (currentPos.x < nextWaypoint.x) currentPos.x++;
                else if (currentPos.y != nextWaypoint.y) currentPos.y += (nextWaypoint.y > currentPos.y) ? 1 : -1;
            }
        }

        return currentPos.y;
    }

    protected override void PlacePlatforms(int[,] mapData, List<Vector2Int> mainPath, int visibleWidth, int height)
    {
        if (mainPath.Count == 0) return;
        Vector2Int lastPlatformEnd = mainPath[0];

        foreach (Vector2Int pathNode in mainPath)
        {
            int distX = Mathf.Abs(pathNode.x - lastPlatformEnd.x);
            int distY = Mathf.Abs(pathNode.y - lastPlatformEnd.y);

            if (distX >= maxJumpDistance || distY >= maxJumpDistance)
            {
                int platY = pathNode.y - 2;
                int platformLength = Random.Range(3, 7);
                bool isClear = true;

                for (int x = pathNode.x - 1; x <= pathNode.x + platformLength; x++)
                {
                    for (int y = platY - 1; y <= platY + 1; y++)
                    {
                        if (x >= 0 && x < visibleWidth && y >= 0 && y < height)
                        {
                            if (mapData[x, y] == 2) { isClear = false; break; }
                        }
                    }
                    if (!isClear) break;
                }

                if (isClear)
                {
                    int actuallyPlaced = 0;
                    for (int i = 0; i < platformLength; i++)
                    {
                        int px = pathNode.x + i;
                        if (px >= 0 && px < visibleWidth && platY >= 0 && platY < height && mapData[px, platY] == 0)
                        {
                            mapData[px, platY] = 2; // ID 2: 발판 타일 매핑
                            actuallyPlaced++;
                        }
                    }
                    if (actuallyPlaced > 0)
                        lastPlatformEnd = new Vector2Int(pathNode.x + actuallyPlaced - 1, platY);
                }
            }
        }
    }

    private void CarveCircle(int[,] mapData, int totalWidth, int height, Vector2Int center, int radius)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (x * x + y * y <= radius * radius)
                {
                    int targetX = center.x + x;
                    int targetY = center.y + y;
                    if (targetX >= 0 && targetX < totalWidth && targetY >= 0 && targetY < height)
                    {
                        mapData[targetX, targetY] = 0; // 0: 허공
                    }
                }
            }
        }
    }
}