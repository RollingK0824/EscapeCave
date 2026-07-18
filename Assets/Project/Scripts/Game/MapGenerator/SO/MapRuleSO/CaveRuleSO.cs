using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CaveRuleSO", menuName = "Scriptable Objects/Map/CaveRuleSO")]
public class CaveRuleSO : BaseMapRuleSO
{
    [Header("동굴 고유 설정")]
    public int tunnelRadius = 6;
    public int minJumpDistance = 4;
    public int maxJumpDistance = 8;

    public override int MinJumpDistance => minJumpDistance;
    public override int MaxJumpDistance => maxJumpDistance;
    public override int MinPlatformHeight => tunnelRadius + 5;
    public override int MaxPlatformHeight => chunkHeight - tunnelRadius - 5;

    protected override void InitializeTerrainBackground()
    {
        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                mapData[x, y] = 1; // Solid wall
            }
        }
    }

    protected override void ApplyThemeSpecificTerrain()
    {
        if (mainPath.Count == 0) return;

        Vector2Int current = mainPath[0];
        CarveCircle(current, tunnelRadius);

        for (int i = 1; i < mainPath.Count; i++)
        {
            Vector2Int next = mainPath[i];
            int steps = Mathf.Max(Mathf.Abs(next.x - current.x), Mathf.Abs(next.y - current.y));
            for (int s = 1; s <= steps; s++)
            {
                float t = (float)s / steps;
                Vector2Int interp = new Vector2Int(
                    Mathf.RoundToInt(Mathf.Lerp(current.x, next.x, t)),
                    Mathf.RoundToInt(Mathf.Lerp(current.y, next.y, t))
                );
                CarveCircle(interp, tunnelRadius);
            }
            current = next;
        }
    }

    private void CarveCircle(Vector2Int center, int radius)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (x * x + y * y <= radius * radius)
                {
                    int targetX = center.x + x;
                    int targetY = center.y + y;
                    if (targetX >= 0 && targetX < chunkWidth && targetY >= 0 && targetY < chunkHeight)
                    {
                        mapData[targetX, targetY] = 0;
                    }
                }
            }
        }
    }
}