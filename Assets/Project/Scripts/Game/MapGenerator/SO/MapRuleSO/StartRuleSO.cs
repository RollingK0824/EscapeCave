using UnityEngine;

[CreateAssetMenu(fileName = "StartRuleSO", menuName = "Scriptable Objects/Map/StartRuleSO")]
public class StartRuleSO : BaseMapRuleSO
{
    [Header("시작 구역 설정")]
    public int floorY = 20;

    protected override int CarveTerrain(int startY)
    {
        // 1. 전체 영역을 1(벽)으로 초기화
        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                mapData[x, y] = 1;
            }
        }

        // 2. 왼쪽 끝(x=0, 1)은 막힌 벽으로 남겨두고 나머지는 평탄하게 깎기
        for (int x = 2; x < chunkWidth; x++)
        {
            for (int y = floorY; y < chunkHeight; y++)
            {
                mapData[x, y] = 0; // 빈 공간 (공기)
            }

            // 길잡이 경로 추가 (부모 클래스 로직 호환성 유지)
            if (x % 5 == 0)
            {
                mainPath.Add(new Vector2Int(x, floorY + 2));
            }
        }

        // 다음 청크가 이 높이에서 이어지도록 반환
        return floorY;
    }

    protected override Vector2Int PlacePlatforms(Vector2Int startPlatform)
    {
        return new Vector2Int(chunkWidth - 1, floorY);
    }
}
