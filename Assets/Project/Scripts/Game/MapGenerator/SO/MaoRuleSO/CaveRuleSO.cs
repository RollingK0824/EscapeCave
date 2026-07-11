using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CaveRuleSO", menuName = "Scriptable Objects/CaveRuleSO")]
public class CaveRuleSO : BaseMapRuleSO
{

    protected override int CarveTerrain(int[,] mapData, List<Vector2Int> mainPath, int width, int height, int startY)
    {
        Vector2Int currentPos = new Vector2Int(0, startY);

        while (currentPos.x < width - 15)
        {
            int nextX = Mathf.Min(currentPos.x + Random.Range(20, 40), width - 1);
            int nextY = Random.Range(tunnelRadius + 5, height - tunnelRadius - 5);
            Vector2Int nextWayPoint = new Vector2Int(nextX, nextY);

            while (currentPos != nextWayPoint)
            {
                if (!mainPath.Contains(currentPos)) mainPath.Add(currentPos);

                // 드릴링 (원형으로 파내기)
                CarveCircle(mapData, width, height, currentPos);

                // 목표를 향해 지그재그/수직으로 파고들기
                if (currentPos.x < nextWayPoint.x && currentPos.y != nextWayPoint.y)
                {
                    if (Random.value < 0.5f) currentPos.x++;
                    else currentPos.y += (nextWayPoint.y > currentPos.y) ? 1 : -1;
                }
                else if (currentPos.x < nextWayPoint.x) currentPos.x++;
                else if (currentPos.y != nextWayPoint.y) currentPos.y += (nextWayPoint.y > currentPos.y) ? 1 : -1;
            }
        }
        return currentPos.y; // 이 청크의 최종 출구 높이 반환
    }

    protected override void PlacePaltforms(int[,] mapData, List<Vector2Int> mainPath, int width, int height)
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

                for (int x = pathNode.x - 1; x <= pathNode.x + platformLength; ++x)
                {
                    for (int y = platY - 1; y <= platY + 1; ++y)
                    {
                        if (x >= 0 && x < width && y >= 0 && y < height)
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
                        if (px >= 0 && px < width && platY >= 0 && platY < height && mapData[px, platY] == 0)
                        {
                            mapData[px, platY] = 2;
                            actuallyPlaced++;
                        }
                    }
                    if (actuallyPlaced > 0)
                        lastPlatformEnd = new Vector2Int(pathNode.x + actuallyPlaced - 1, platY);
                }
            }
        }
    }

    private void CarveCircle(int[,] mapData, int width, int height, Vector2Int center)
    {
        for (int x = -tunnelRadius; x <= tunnelRadius; ++x)
        {
            for (int y = -tunnelRadius; y <= tunnelRadius; ++y)
            {
                int targetX = center.x + x;
                int targetY = center.y + y;
                if (targetX >= 0 && targetX < width & targetY >= 0 && targetY < height)
                {
                    mapData[targetX, targetY] = 0;
                }
            }
        }
    }
}
