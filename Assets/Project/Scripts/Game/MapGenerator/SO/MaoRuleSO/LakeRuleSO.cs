using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LakeRuleSO", menuName = "Scriptable Objects/Map/LakeRuleSO")]
public class LakeRuleSO : BaseMapRuleSO
{
    private struct PondArea
    {
        public int xMin;
        public int xMax;
        public int yMin;
        public int yMax;
    }
    
    [System.NonSerialized] private List<PondArea> activePonds = new List<PondArea>();

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

        if (activePonds == null) activePonds = new List<PondArea>();
        activePonds.Clear();

        int pondCount = chunkRandom.Next(minPonds, maxPonds + 1);
        for (int i = 0; i < pondCount; i++)
        {
            int pondCenter = chunkRandom.Next(blendRange + 10, chunkWidth - blendRange - 10);
            int pondWidth = chunkRandom.Next(minPondWidth, maxPondWidth + 1);
            int pondDepth = chunkRandom.Next(minPondDepth, maxPondDepth + 1);
            int pondWaterLevel = baseFloorY - 2;

            activePonds.Add(new PondArea {
                xMin = pondCenter - pondWidth - 3,
                xMax = pondCenter + pondWidth + 3,
                yMin = baseFloorY - pondDepth - 3,
                yMax = pondWaterLevel
            });

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
                        for (int y = newFloorY; y <= pondWaterLevel; y++)
                        {
                            if (y >= 0 && y < chunkHeight && mapData[x, y] == 0)
                            {
                                mapData[x, y] = 3;
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

    protected override void RenderToTilemap()
    {
        TileBase[] tileArray = new TileBase[chunkWidth * chunkHeight];
        TileBase[] waterTileArray = new TileBase[chunkWidth * chunkHeight];
        
        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                int tileID = mapData[x, y];
                int index = x + y * chunkWidth;
                
                if (tileID == 2 || tileID == 4 || tileID == 5) tileID = 0;
                
                bool isWaterRect = false;
                if (activePonds != null)
                {
                    foreach (var p in activePonds)
                    {
                        if (x >= p.xMin && x <= p.xMax && y >= p.yMin && y <= p.yMax)
                        {
                            isWaterRect = true;
                            break;
                        }
                    }
                }
                
                if (isWaterRect)
                {
                    waterTileArray[index] = (themeTiles.Count >= 3) ? themeTiles[2] : null;
                }
                else
                {
                    waterTileArray[index] = null;
                }

                if (tileID == 3)
                {
                    tileID = 0;
                }

                tileArray[index] = (tileID > 0 && tileID <= themeTiles.Count) ? themeTiles[tileID - 1] : null;
            }
        }
        
        BoundsInt bounds = new BoundsInt(offsetX, 0, 0, chunkWidth, chunkHeight, 1);
        globalTilemap.SetTilesBlock(bounds, tileArray);
        if (waterTilemap != null)
        {
            waterTilemap.SetTilesBlock(bounds, waterTileArray);
        }
    }
}