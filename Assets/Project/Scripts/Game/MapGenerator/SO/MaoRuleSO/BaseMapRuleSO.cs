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

    /// <summary>
    /// 전체 청크 생성 파이프라인의 실행 순서를 보장
    /// </summary>
    public int GenerateChunk(Tilemap tilemap, int[,] mapData, List<Vector2Int> mainPath, int startY, float seed)
    {
        int width = mapData.GetLength(0);
        int height = mapData.GetLength(1);

        // Pass 0: 가로 전체(Width + Padding)를 단단한 벽(1)으로 채움
        InitializeMap(mapData, width, height);

        int exitY = CarveTerrain(mapData, mainPath, width, height, startY, seed);

        // Pass 2: 플랫폼 배치 (화면에 보이는 구역 중심)
        PlacePlatforms(mapData, mainPath, chunkWidth, height);

        // Pass 4: 최종 렌더링 (화면에 보이는 chunkWidth 만큼만 짤라서 그림)
        RenderToTilemap(tilemap, mapData, chunkWidth, height);

        return exitY;
    }

    protected virtual void InitializeMap(int[,] mapData, int totalWidth, int height)
    {
        for (int x = 0; x < totalWidth; x++)
        {
            for (int y = 0; y < height; y++)
            {
                mapData[x, y] = 1;
            }
        }
    }

    private void RenderToTilemap(Tilemap tilemap, int[,] mapData, int visibleWidth, int height)
    {
        TileBase[] tileArray = new TileBase[visibleWidth * height];
        int tileCount = themeTiles.Count;

        for (int x = 0; x < visibleWidth; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int tileID = mapData[x, y];
                int index = x + y * visibleWidth;

                if (tileID == 0) tileArray[index] = null;
                else if (tileID > 0 && tileID <= tileCount) tileArray[index] = themeTiles[tileID - 1];
                else tileArray[index] = null;
            }
        }

        BoundsInt bounds = new BoundsInt(0, 0, 0, visibleWidth, height, 1);
        tilemap.SetTilesBlock(bounds, tileArray);
    }

    // 자식 클래스가 구현해야 할 추상 및 가상 파이프라인
    protected abstract int CarveTerrain(int[,] mapData, List<Vector2Int> mainPath, int totalWidth, int height, int startY, float seed);
    protected virtual void PlacePlatforms(int[,] mapData, List<Vector2Int> mainPath, int visibleWidth, int height) { }
}