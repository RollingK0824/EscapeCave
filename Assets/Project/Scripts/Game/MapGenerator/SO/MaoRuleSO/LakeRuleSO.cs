using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "LakeRuleSO", menuName = "Scriptable Objects/Map/LakeRuleSO")]
public class LakeRuleSO : BaseMapRuleSO
{
    [Header("호수 고유 설정")]
    public int waterLevel = 15;
    public float floorNoiseScale = 0.05f;
    public int maxFloorHeight = 10;
    public int minFloorHeight = 2;
    public int ceilingHeight = 45;

    protected override int CarveTerrain(int[,] mapData, List<Vector2Int> mainPath, int totalWidth, int height, int startY, float seed)
    {
        Random.InitState((int)seed);

        for (int x = 0; x < totalWidth; x++)
        {
            float noise = Mathf.PerlinNoise((x * floorNoiseScale) + seed, 0f);
            int floorY = minFloorHeight + Mathf.FloorToInt(noise * (maxFloorHeight - minFloorHeight));

            for (int y = floorY; y < ceilingHeight; y++)
            {
                if (y < height) mapData[x, y] = 0;
            }

            for (int y = floorY; y <= waterLevel; y++)
            {
                if (y < height && mapData[x, y] == 0) mapData[x, y] = 3;
            }
        }
        return waterLevel + 4;
    }

    protected override void PlacePlatforms(int[,] mapData, List<Vector2Int> mainPath, int visibleWidth, int height)
    {
        int currentX = Random.Range(3, 7);

        while (currentX < visibleWidth - 5)
        {
            currentX += Random.Range(3, 7);

            int platLength = Random.Range(4, 9);

            int platY = waterLevel + Random.Range(3, 10);

            for (int i = 0; i < platLength; i++)
            {
                int px = currentX + i;
                if (px < visibleWidth && mapData[px, platY] == 0)
                {
                    mapData[px, platY] = 2; // 발판 타일 적용
                }
            }

            currentX += platLength;
        }
    }
}