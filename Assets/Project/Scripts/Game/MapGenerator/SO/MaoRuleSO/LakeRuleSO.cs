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

    // [핵심 변경] 완전히 깔끔한 평탄형 발판 배치 로직
    protected override void PlacePlatforms(int[,] mapData, List<Vector2Int> mainPath, int visibleWidth, int height)
    {
        int currentX = Random.Range(3, 7); // 첫 발판이 시작될 여백

        while (currentX < visibleWidth - 5)
        {
            // 1. 발판 간의 가로 간격 (너무 촘촘하지 않게 징검다리 틈새 확보)
            currentX += Random.Range(3, 7);

            // 2. 발판의 가로 길이 (N)
            int platLength = Random.Range(4, 9);

            // 3. 발판의 높이 (수면 위 ~ 천장 아래 사이의 무작위 높이에 '고정')
            int platY = waterLevel + Random.Range(3, 10);

            // 4. 발판 생성 (N x 1 형태의 일자 발판)
            for (int i = 0; i < platLength; i++)
            {
                int px = currentX + i;
                if (px < visibleWidth && mapData[px, platY] == 0)
                {
                    mapData[px, platY] = 2; // 발판 타일 적용
                }
            }

            // 발판 길이만큼 X축을 전진시켜 다음 발판이 겹치지 않게 만듦
            currentX += platLength;
        }
    }
}