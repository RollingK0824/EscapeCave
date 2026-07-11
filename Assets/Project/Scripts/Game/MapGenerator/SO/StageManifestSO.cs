using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct TileModifier
{
    public string ruleName;     // 규칙 서술 명칭
    public Vector2Int offset;   // 타일 스폰 중심점 기준 상대 좌표
    public Vector2Int size;     // 변형할 지형의 격자 크기 (가로 세로)
    public int targetTileID;    // 변경하고자 하는 타일 ID
}

[System.Serializable]
public struct StageEvent
{
    public string eventName;                    // 기믹 이름
    public GameObject prefab;                   // 스폰할 오브젝트 프리팹
    [Range(0f, 1f)] public float spawnChance;   // 스폰 확률

    [Tooltip("직전 이벤트 발생 후 최소 전진 거리")]
    public int minProgressDistance;             

    [Header("지형 제어 명령 배열 (Tile Modification Rules")]
    public TileModifier[] terrainModifiers;
}

[CreateAssetMenu(fileName = "StageManifestSO", menuName = "Scriptable Objects/Map/StageManifestSO")]
public class StageManifestSO : ScriptableObject
{
    public string stageName;
    public int stageWidth = 150;
    public int stageHeight = 80;
    public int tunnelRadius = 6;

    public float noiseScale = 0.05f;   // 값이 작을수록 완만하고 넓은 곡선, 클수록 뾰족하고 잦은 굴곡
    public int maxBottomHeight = 8;    // 바닥 지형이 위로 솟구칠 수 있는 최대 높이 (칸 수)
    public int minBottomHeight = 3;    // 아무리 낮아도 보장되는 최소 바닥 두께

    [Header("순서대로 등장할 오브젝트 리스트")]
    public List<StageEvent> eventSequence;
}
