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
        // 1. 메모리 재사용 세팅: 배열이 없거나, SO의 크기 세팅이 바뀌었을 때만 새로 할당
        if (_mapData == null || _mapData.GetLength(0) != rule.chunkWidth || _mapData.GetLength(1) != rule.chunkHeight)
        {
            _mapData = new int[rule.chunkWidth, rule.chunkHeight];
        }

        // 2. 기존 데이터 초기화 (new를 쓰지 않고 비우기만 함)
        _mainPath.Clear();
        chunkTilemap.ClearAllTiles();

        // _mapData는 BaseMapRuleSO의 InitializeMap에서 전부 1(벽)로 덮어씌워지므로 
        // 별도로 Array.Clear()를 할 필요가 없습니다.

        // 3. 주입받은 룰(SO)의 템플릿 메서드 실행!
        return rule.GenerateChunk(chunkTilemap, _mapData, _mainPath, startY, seed);
    }
}
