using UnityEngine;

[CreateAssetMenu(fileName = "StartRuleSO", menuName = "Scriptable Objects/Map/StartRuleSO")]
public class StartRuleSO : BaseMapRuleSO
{
    [Header("시작 구역 설정")]
    public int floorY = 20;

    protected override void InitializeTerrainBackground()
    {
        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                mapData[x, y] = 1;
            }
        }
    }

    protected override void ApplyThemeSpecificTerrain()
    {
        for (int x = 2; x < chunkWidth; x++)
        {
            for (int y = floorY; y < chunkHeight; y++)
            {
                mapData[x, y] = 0; // 빈 공간 (공기)
            }
        }
    }

    protected override Vector2Int GeneratePathAndPlatforms(Vector2Int startPlatform)
    {
        for (int x = 5; x < chunkWidth; x += 5)
        {
            mainPath.Add(new Vector2Int(x, floorY + 2));
        }
        return new Vector2Int(chunkWidth - 1, floorY);
    }
}
