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

    [Header("점프 간격 설정")]
    public int minJumpDistance = 4;
    public int maxJumpDistance = 8;

    [Header("물 속 스폰 설정")]
    public List<SpawnRule> waterSpawns;

    public override int MinJumpDistance => minJumpDistance;
    public override int MaxJumpDistance => maxJumpDistance;
    public override int MinPlatformHeight => baseFloorY + 3;
    public override int MaxPlatformHeight => baseFloorY + 9;

    protected override void InitializeTerrainBackground()
    {
        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                if (y < baseFloorY) mapData[x, y] = 1; // Solid ground below water table
                else mapData[x, y] = 0; // Air above
            }
        }
    }

    protected override void ApplyThemeSpecificTerrain()
    {
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
                                mapData[x, y] = 3; // Water
                            }
                        }
                    }
                }
            }
        }
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

    protected override void SpawnThemeSpecificObjects()
    {
        if (waterSpawns == null || waterSpawns.Count == 0) return;

        int lastFishSpawnX = -minSpawnGapX;

        for (int x = 0; x < chunkWidth; x++)
        {
            int minWaterY = -1;
            int maxWaterY = -1;

            // 물(ID = 3) 영역 탐지
            for (int y = 0; y < chunkHeight; y++)
            {
                if (mapData[x, y] == 3)
                {
                    if (minWaterY == -1) minWaterY = y;
                    maxWaterY = y;
                }
            }

            if (minWaterY != -1 && maxWaterY >= minWaterY)
            {
                if (x - lastFishSpawnX >= minSpawnGapX)
                {
                    int randomY = chunkRandom.Next(minWaterY, maxWaterY + 1);
                    if (TrySpawnObject(waterSpawns, x, randomY))
                    {
                        lastFishSpawnX = x;
                    }
                }
            }
        }
    }
}