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
        int lastPlatformStartX = startPlatform.x;
        if (platformConfigurations == null || platformConfigurations.Count == 0) return lastPlatformEnd;

        foreach (Vector2Int pathNode in mainPath)
        {
            int distX = Mathf.Abs(pathNode.x - lastPlatformEnd.x);
            int distY = Mathf.Abs(pathNode.y - lastPlatformEnd.y);
            
            int platY = pathNode.y - 2;
            int heightDiff = Mathf.Abs(platY - lastPlatformEnd.y);
            
            bool isHeightCritical = (heightDiff >= 4);
            bool forceSpawn = (distX >= maxJumpDistance || distY >= maxJumpDistance || isHeightCritical);

            if (distX >= minJumpDistance || distY >= minJumpDistance || forceSpawn)
            {
                PlatformSpawnRule rule = platformConfigurations[chunkRandom.Next(0, platformConfigurations.Count)];
                if (!forceSpawn && chunkRandom.NextDouble() > rule.spawnChance) continue;

                int chosenLength = chunkRandom.Next(rule.minLength, rule.maxLength + 1);
                if (chosenLength < 1) chosenLength = 1;
                int requiredLength = chosenLength;

                int bestStartX = -1;
                bool found = false;

                for (int attempt = 0; attempt < 2; attempt++)
                {
                    if (attempt == 1)
                    {
                        if (!forceSpawn) break;
                        rule = platformConfigurations[0]; // fallback to static
                        rule.type = PlatformType.Static;
                        requiredLength = 2;
                        chosenLength = 2;
                    }

                    for (int offset = 0; offset <= 5; offset++)
                    {
                        int[] signs = (offset == 0) ? new int[] { 0 } : new int[] { 1, -1 };
                        foreach (int sign in signs)
                        {
                            int testStartX = pathNode.x + offset * sign;
                            
                            // 수직 상승 시 머리 박치기 방지 (지그재그 배치 강제)
                            bool overlap = !(testStartX > lastPlatformEnd.x || (testStartX + requiredLength - 1) < lastPlatformStartX);
                            if (overlap && heightDiff >= 2) continue;

                            int checkMinX = testStartX - 1;
                            int checkMaxX = testStartX + requiredLength;
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
                                    else { isClear = false; break; }
                                }
                                if (!isClear) break;
                            }

                            if (isClear)
                            {
                                bestStartX = testStartX;
                                found = true;
                                break;
                            }
                        }
                        if (found) break;
                    }
                    if (found) break;
                }

                if (found)
                {
                    int markID = 2;
                    int checkMaxX = bestStartX + requiredLength;
                    int checkMaxY = platY + 1;

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

                    for (int x = bestStartX; x < checkMaxX; x++)
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
                        localX = bestStartX,
                        localY = platY,
                        chosenLength = chosenLength,
                        rule = rule
                    });

                    lastPlatformStartX = bestStartX;
                    lastPlatformEnd = new Vector2Int(bestStartX + requiredLength - 1, platY);
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