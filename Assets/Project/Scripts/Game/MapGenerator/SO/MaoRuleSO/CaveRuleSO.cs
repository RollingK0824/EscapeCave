using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CaveRuleSO", menuName = "Scriptable Objects/Map/CaveRuleSO")]
public class CaveRuleSO : BaseMapRuleSO
{
    [Header("동굴 고유 설정")]
    public int tunnelRadius = 6;
    public int minJumpDistance = 4;
    public int maxJumpDistance = 8;

    protected override int CarveTerrain(int startY)
    {
        Vector2Int currentPos = new Vector2Int(0, startY);

        while (currentPos.x < chunkWidth - 5)
        {
            int nextX = Mathf.Min(currentPos.x + chunkRandom.Next(20, 40), chunkWidth - 1);
            int nextY = chunkRandom.Next(tunnelRadius + 5, chunkHeight - tunnelRadius - 5);
            Vector2Int nextWaypoint = new Vector2Int(nextX, nextY);

            while (currentPos != nextWaypoint)
            {
                if (!mainPath.Contains(currentPos)) mainPath.Add(currentPos);

                CarveCircle(currentPos, tunnelRadius);

                if (currentPos.x < nextWaypoint.x && currentPos.y != nextWaypoint.y)
                {
                    if (chunkRandom.NextDouble() < 0.5) currentPos.x++;
                    else currentPos.y += (nextWaypoint.y > currentPos.y) ? 1 : -1;
                }
                else if (currentPos.x < nextWaypoint.x) currentPos.x++;
                else if (currentPos.y != nextWaypoint.y) currentPos.y += (nextWaypoint.y > currentPos.y) ? 1 : -1;
            }
        }
        return currentPos.y;
    }

    protected override Vector2Int PlacePlatforms(Vector2Int startPlatform)
    {
        Vector2Int lastPlatformEnd = startPlatform;
        if (platformConfigurations == null || platformConfigurations.Count == 0) return lastPlatformEnd;

        foreach (Vector2Int pathNode in mainPath)
        {
            int distX = Mathf.Abs(pathNode.x - lastPlatformEnd.x);
            int distY = Mathf.Abs(pathNode.y - lastPlatformEnd.y);

            if (distX >= maxJumpDistance || distY >= maxJumpDistance)
            {
                int platY = pathNode.y - 2;
                
                PlatformSpawnRule rule = platformConfigurations[chunkRandom.Next(0, platformConfigurations.Count)];
                if (chunkRandom.NextDouble() > rule.spawnChance) continue;

                // 하이브리드 무작위 길이 결정 (방어코드 적용)
                int chosenLength = chunkRandom.Next(rule.minLength, rule.maxLength + 1);
                if (chosenLength < 1) chosenLength = 1;

                int requiredLength = chosenLength;
                int startX = pathNode.x;
                
                int checkMinX = startX - 1;
                int checkMaxX = startX + requiredLength;
                int checkMinY = platY - 1;
                int checkMaxY = platY + 1;
                
                int markID = 2;

                if (rule.type == PlatformType.Moving)
                {
                    markID = 4;
                    if (rule.moveDirection == MoveDirection.Horizontal) checkMaxX += rule.moveRange;
                    else checkMaxY += rule.moveRange;
                }
                else if (rule.type == PlatformType.Pullable)
                {
                    markID = 5;
                    checkMaxX += Mathf.CeilToInt(rule.pullLimit);
                }

                bool isClear = true;
                for (int x = checkMinX; x <= checkMaxX; x++)
                {
                    for (int y = checkMinY; y <= checkMaxY; y++)
                    {
                        if (x >= 0 && x < chunkWidth && y >= 0 && y < chunkHeight)
                        {
                            int t = mapData[x, y];
                            if (t == 1 || t == 2 || t == 4 || t == 5)
                            {
                                isClear = false;
                                break;
                            }
                        }
                    }
                    if (!isClear) break;
                }

                if (isClear)
                {
                    for (int x = startX; x < checkMaxX; x++)
                    {
                        for (int y = platY; y <= checkMaxY - 1; y++)
                        {
                            if (x >= 0 && x < chunkWidth && y >= 0 && y < chunkHeight)
                            {
                                mapData[x, y] = markID;
                            }
                        }
                    }

                    pendingPlatforms.Add(new PlatformSpawnData
                    {
                        localX = startX,
                        localY = platY,
                        chosenLength = chosenLength,
                        rule = rule
                    });

                    lastPlatformEnd = new Vector2Int(startX + requiredLength - 1, platY);
                }
            }
        }
        return lastPlatformEnd;
    }

    private void CarveCircle(Vector2Int center, int radius)
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