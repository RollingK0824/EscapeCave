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

    protected override int CarveTerrain(int startY)
    {
        for (int x = 0; x < chunkWidth; x++)
        {
            float noise = Mathf.PerlinNoise((x * floorNoiseScale) + currentSeed, 0f);
            int floorY = minFloorHeight + Mathf.FloorToInt(noise * (maxFloorHeight - minFloorHeight));

            for (int y = floorY; y < ceilingHeight; y++)
            {
                if (y < chunkHeight) mapData[x, y] = 0;
            }

            for (int y = floorY; y <= waterLevel; y++)
            {
                if (y < chunkHeight && mapData[x, y] == 0) mapData[x, y] = 3;
            }

            if (x == 15) mainPath.Add(new Vector2Int(x, waterLevel + 4));
        }
        return waterLevel + 4;
    }

    protected override Vector2Int PlacePlatforms(Vector2Int startPlatform)
    {
        Vector2Int lastPlatformEnd = startPlatform;

        int currentX = lastPlatformEnd.x + chunkRandom.Next(3, 7);
        if (currentX < 0) currentX = 0;

        while (currentX < chunkWidth - 5)
        {
            int platLength = chunkRandom.Next(4, 9);
            int platY = waterLevel + chunkRandom.Next(3, 10);

            for (int i = 0; i < platLength; i++)
            {
                int px = currentX + i;
                if (px < chunkWidth && mapData[px, platY] == 0)
                {
                    mapData[px, platY] = 2;
                }
            }

            lastPlatformEnd = new Vector2Int(currentX + platLength - 1, platY);
            currentX += platLength + chunkRandom.Next(3, 7);
        }

        return lastPlatformEnd;
    }
}