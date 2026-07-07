using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CaveRuleSO", menuName = "Scriptable Objects/CaveRuleSO")]
public class CaveRuleSO : BaseMapRuleSO
{
    [Header("동굴 고유 설정")]
    public int tunnelRadius = 6;
    public int minJumpDistance = 4;
    public int maxJumpDistance = 8;

    protected override int CarveTerrain(int[,] mapData, int startY)
    {
        Vector2Int currentPos = new Vector2Int(0, startY);

        while (currentPos.x < chunkWidth - 5)
        {
            int nextX = Mathf.Min(currentPos.x + Random.Range(20, 40), chunkWidth - 1);
            int nextY = Random.Range(tunnelRadius + 5, chunkHeight - tunnelRadius - 5);
            Vector2Int nextWaypoint = new Vector2Int(nextX, nextY);

            while (currentPos != nextWaypoint)
            {
                if (!mainPath.Contains(currentPos)) mainPath.Add(currentPos);

                CarveCircle(mapData, currentPos, tunnelRadius);

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

    protected override Vector2Int PlacePlatforms(int[,] mapData, Vector2Int startPlatform)
    {
        Vector2Int lastPlatformEnd = startPlatform;

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
                        if (x >= 0 && x < chunkWidth && y >= 0 && y < chunkHeight)
                            if (mapData[x, y] == 2) { isClear = false; break; }
                    }
                    if (!isClear) break;
                }

                if (isClear)
                {
                    int actuallyPlaced = 0;
                    for (int i = 0; i < platformLength; i++)
                    {
                        int px = pathNode.x + i;
                        if (px >= 0 && px < chunkWidth && platY >= 0 && platY < chunkHeight && mapData[px, platY] == 0)
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
        return lastPlatformEnd;
    }

    private void CarveCircle(int[,] mapData, Vector2Int center, int radius)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (x * x + y * y <= radius * radius)
                {
                    int targetX = center.x + x;
                    int targetY = center.y + y;
                    if (targetX >= 0 && targetX < chunkWidth && targetY >= 0 && targetY < chunkHeight)
                    {
                        mapData[targetX, targetY] = 0;
                    }
                }
            }
        }
    }
}