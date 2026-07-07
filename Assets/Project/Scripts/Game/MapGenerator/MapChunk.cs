using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapChunk : MonoBehaviour
{
    public Tilemap chunkTilemap;
    private int[,] _mapData;
    private List<Vector2Int> _mainPath = new List<Vector2Int>();

    /// <summary>
    /// 맵 생성기가 호출할 함수
    /// </summary>
    /// <param name="rule">생성할 맵 형태(로직)</param>
    /// <param name="startY">시작 높이</param>
    /// <returns></returns>
    public int BuildMap(BaseMapRuleSO rule, int startY, float seed)
    {
        if (_mapData == null || _mapData.GetLength(0) != rule.chunkWidth || _mapData.GetLength(1) != rule.chunkHeight)
        {
            _mapData = new int[rule.chunkWidth, rule.chunkHeight];
        }

        _mainPath.Clear();
        chunkTilemap.ClearAllTiles();

        return rule.GenerateChunk(chunkTilemap, _mapData, _mainPath, startY, seed);
    }
}
