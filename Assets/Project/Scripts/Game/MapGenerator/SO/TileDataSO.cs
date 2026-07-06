using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "TileDataSO", menuName = "Scriptable Objects/Map/TileDataSO")]
public class TileDataSO : ScriptableObject
{
    public string themeName;
    [Tooltip("Index 0: Empty, 1: Wall, 2: Platform, 3: MovedPlatform, 4: Water")]
    public TileBase[] tiles;

    // 타일 ID가 현재 등록된 사전 범위 내에 있는지 검증하는 함수
    public bool IsValidTileID(int id)
    {
        return id >= 0 && id < tiles.Length;
    }
}
