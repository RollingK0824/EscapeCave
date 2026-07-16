using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LakeRuleSO", menuName = "Scriptable Objects/Map/LakeRuleSO")]
public class LakeRuleSO : BaseMapRuleSO
{
    [Header("호수 웅덩이 설정")]
    public int baseFloorY = 20;
    public int minPonds = 2;
    public int maxPonds = 4;
    public int minPondWidth = 15;
    public int maxPondWidth = 35;
    public int minPondDepth = 5;
    public int maxPondDepth = 12;

    protected override int CarveTerrain(int startY)
    {
        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = baseFloorY; y < chunkHeight; y++)
            {
                mapData[x, y] = 0;
            }
            if (x == 15) mainPath.Add(new Vector2Int(x, baseFloorY + 2));
        }

        int pondCount = chunkRandom.Next(minPonds, maxPonds + 1);
        for (int i = 0; i < pondCount; i++)
        {
            int pondCenter = chunkRandom.Next(blendRange + 10, chunkWidth - blendRange - 10);
            int pondWidth = chunkRandom.Next(minPondWidth, maxPondWidth + 1);
            int pondDepth = chunkRandom.Next(minPondDepth, maxPondDepth + 1);
            int pondWaterLevel = baseFloorY - 2;

            for (int x = pondCenter - pondWidth; x <= pondCenter + pondWidth; x++)
            {
                if (x >= 0 && x < chunkWidth)
                {
                    float normalizedDist = (float)Mathf.Abs(x - pondCenter) / pondWidth;
                    int depthAtX = Mathf.RoundToInt(pondDepth * (1.0f - (normalizedDist * normalizedDist)));
                    
                    if (depthAtX > 0)
                    {
                        int newFloorY = baseFloorY - depthAtX;
                        for (int y = newFloorY; y < baseFloorY; y++)
                        {
                            if (y >= 0 && y < chunkHeight) mapData[x, y] = 0;
                        }
                        int startPondX = pondCenter - pondWidth;
                        for (int y = newFloorY; y <= pondWaterLevel; y++)
                        {
                            if (y >= 0 && y < chunkHeight && mapData[x, y] == 0)
                            {
                                if (y == pondWaterLevel)
                                {
                                    // 수면 타일(ID 3)은 5칸 간격으로 띄엄띄엄 배치
                                    if ((x - startPondX) % 5 == 0)
                                    {
                                        mapData[x, y] = 3;
                                    }
                                }
                                else
                                {
                                    // 물속은 빈틈 없이 물속 타일(ID 6)로 채움
                                    mapData[x, y] = 6;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        return baseFloorY + 2;
    }

    protected override Vector2Int PlacePlatforms(Vector2Int startPlatform)
    {
        Vector2Int lastPlatformEnd = startPlatform;
        if (platformConfigurations == null || platformConfigurations.Count == 0) return lastPlatformEnd;

        int currentX = lastPlatformEnd.x + chunkRandom.Next(3, 7);
        if (currentX < 0) currentX = 0;

        while (currentX < chunkWidth - 5)
        {
            int platY = baseFloorY + chunkRandom.Next(3, 10);

            PlatformSpawnRule rule = platformConfigurations[chunkRandom.Next(0, platformConfigurations.Count)];
            if (chunkRandom.NextDouble() > rule.spawnChance)
            {
                currentX += chunkRandom.Next(4, 9);
                continue;
            }

            // 하이브리드 무작위 길이 결정 (방어코드 적용)
            int chosenLength = chunkRandom.Next(rule.minLength, rule.maxLength + 1);
            if (chosenLength < 1) chosenLength = 1;

            int requiredLength = chosenLength;
            int startX = currentX;

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
            currentX += requiredLength + chunkRandom.Next(3, 7);
        }

        return lastPlatformEnd;
    }
}