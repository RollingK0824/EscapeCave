using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BaseMapRuleSO", menuName = "Scriptable Objects/Map/BaseMapRuleSO")]
public abstract class BaseMapRuleSO : ScriptableObject
{
    [Header("공통 맵 크기 설정")]
    public int chunkWidth = 300;
    public int chunkHeight = 50;

    [Header("테마별 타일 셋 (1번부터 순서대로 인스펙터 매핑)")]
    public List<TileBase> themeTiles = new List<TileBase>();

    protected float currentSeed;
    protected List<Vector2Int> mainPath = new List<Vector2Int>();

    /// <summary>
    /// 전체 청크 생성 파이프라인의 실행 순서를 보장
    /// </summary>
    public int GenerateChunk(Tilemap globalTilemap, int[,] mapData, int offsetX, int startY, float seed, Vector2Int startPlatform, out Vector2Int endPlatform)
    {
        currentSeed = seed;
        Random.InitState((int)seed);
        mainPath.Clear();

        InitializeMap(mapData);
        int exitY = CarveTerrain(mapData, startY);

        ForceTransitionTunnel(mapData, startY);
        endPlatform = PlacePlatforms(mapData, startPlatform);

        RenderToTilemap(globalTilemap, mapData, offsetX);

        return exitY;
    }

    protected virtual void InitializeMap(int[,] mapData)
    {
        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkHeight; y++) mapData[x, y] = 1;
        }
    }

    private void RenderToTilemap(Tilemap globalTilemap, int[,] mapData, int offsetX)
    {
        TileBase[] tileArray = new TileBase[chunkWidth * chunkHeight];
        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                int tileID = mapData[x, y];
                int index = x + y * chunkWidth;
                tileArray[index] = (tileID > 0 && tileID <= themeTiles.Count) ? themeTiles[tileID - 1] : null;
            }
        }
        BoundsInt bounds = new BoundsInt(offsetX, 0, 0, chunkWidth, chunkHeight, 1);
        globalTilemap.SetTilesBlock(bounds, tileArray);
    }

    private void ForceTransitionTunnel(int[,] mapData, int startY)
    {
        if (mainPath.Count == 0) return;

        int targetY = mainPath[0].y;
        int transitionLength = 15;

        for (int x = 0; x < transitionLength; x++)
        {
            float t = (float)x / transitionLength;
            int currentY = Mathf.RoundToInt(Mathf.Lerp(startY, targetY, t));
            int radius = 5;

            for (int cx = -radius; cx <= radius; cx++)
            {
                for (int cy = -radius; cy <= radius; cy++)
                {
                    if (cx * cx + cy * cy <= radius * radius)
                    {
                        int px = x + cx;
                        int py = currentY + cy;
                        if (px >= 0 && px < chunkWidth && py >= 0 && py < chunkHeight)
                        {
                            if (mapData[px, py] == 1) mapData[px, py] = 0;
                        }
                    }
                }
            }
        }
    }

    protected abstract int CarveTerrain(int[,] mapData, int startY);
    protected abstract Vector2Int PlacePlatforms(int[,] mapData, Vector2Int startPlatform);
}