using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BaseMapRuleSO", menuName = "Scriptable Objects/BaseMapRuleSO")]
public abstract class BaseMapRuleSO : ScriptableObject
{
    [Header("공통 타일 에셋")]
    public TileBase wallTile;
    public TileBase platformTile;

    [Header("맵 설정")]
    public int chunkWidth = 300;
    public int chunkHeight = 50;
    public int tunnelRadius = 6;
    public int minJumpDistance = 4;
    public int maxJumpDistance = 8;

    public int GenerateChunk(Tilemap tileMap, int[,] mapData, List<Vector2Int> mainPath, int startY)
    {
        int width = mapData.GetLength(0);
        int height = mapData.GetLength(1);

        InitializeMap(mapData, width, height);

        int exitY = CarveTerrain(mapData, mainPath, width, height, startY);

        PlacePaltforms(mapData, mainPath, width, height);

        RenderToTilemap(tileMap, mapData, width, height);

        return exitY;
    }

    protected virtual void InitializeMap(int[,] mapData, int width, int height)
    {
        for (int x = 0; x < width; ++x)
        {
            for (int y = 0; y < height; ++y)
            {
                mapData[x, y] = 1;
            }
        }
    }

    private void RenderToTilemap(Tilemap tileMap, int[,] mapData, int width, int height)
    {
        TileBase[] tileArray = new TileBase[width * height];
        for (int x = 0; x < width; ++x)
        {
            for (int y = 0; y < height; ++y)
            {
                int id = mapData[x, y];
                int index = x + y * width;
                if (id == 1) tileArray[index] = wallTile;
                else if (id == 2) tileArray[index] = platformTile;
                else tileArray[index] = null;
            }
        }
        BoundsInt bounds =  new BoundsInt(0, 0, 0, width, height, 1);
        tileMap.SetTilesBlock(bounds, tileArray);
    }

    protected abstract int CarveTerrain(int[,] mapData, List<Vector2Int> mainPath, int width, int height, int startY);
    protected virtual void PlacePaltforms(int[,] mapData, List<Vector2Int> mainPath, int width, int height) { }
}