using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AbyssRuleSO", menuName = "Scriptable Objects/Map/AbyssRuleSO")]
public class AbyssRuleSO : BaseMapRuleSO
{
    [Header("심연 고유 설정")]
    public int ceilingHeight = 45;
    public int minJumpDistance = 4;
    public int maxJumpDistance = 8;

    protected override int CarveTerrain(int startY)
    {
        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < ceilingHeight; y++)
            {
                if (y < chunkHeight) mapData[x, y] = 0;
            }
        }
        
        int currentY = startY;
        for (int x = 15; x < chunkWidth - 15; x += chunkRandom.Next(10, 20))
        {
            currentY += chunkRandom.Next(-5, 6);
            if (currentY < 5) currentY = 5;
            if (currentY > ceilingHeight - 10) currentY = ceilingHeight - 10;
            mainPath.Add(new Vector2Int(x, currentY));
        }

        return currentY;
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
                
                // 1. 하이브리드 무작위 길이 결정 (방어코드 적용)
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

                    // 2. 대기열에 결정된 임의 길이(chosenLength) 삽입
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
}
