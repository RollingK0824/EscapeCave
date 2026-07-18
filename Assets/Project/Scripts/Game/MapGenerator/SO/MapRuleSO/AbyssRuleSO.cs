using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AbyssRuleSO", menuName = "Scriptable Objects/Map/AbyssRuleSO")]
public class AbyssRuleSO : BaseMapRuleSO
{
    [Header("심연 고유 설정")]
    public int ceilingHeight = 45;
    public int minJumpDistance = 4;
    public int maxJumpDistance = 8;

    public override int MinJumpDistance => minJumpDistance;
    public override int MaxJumpDistance => maxJumpDistance;
    public override int MinPlatformHeight => 5;
    public override int MaxPlatformHeight => ceilingHeight - 10;

    protected override void InitializeTerrainBackground()
    {
        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                if (y < ceilingHeight) mapData[x, y] = 0; // Empty air (Abyss)
                else mapData[x, y] = 1; // Solid ceiling
            }
        }
    }

    protected override void ApplyThemeSpecificTerrain()
    {
        // Abyss has no specific terrain carving below the platforms, it's just empty air.
    }
}
