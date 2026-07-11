using UnityEngine;

/// <summary>
/// 인벤토리에 들어가는 아이템의 종류.
/// 새 소모품/효과를 추가할 때 여기에 값만 늘리면 됩니다.
/// </summary>
public enum ItemType
{
    None,
    Flight, // 하늘을 날게 하는 아이템
}

/// <summary>
/// 아이템 하나를 정의하는 데이터. Project 창에서
/// Create > Item > ItemData 로 에셋을 만들어 사용합니다.
/// </summary>
[CreateAssetMenu(fileName = "New Item", menuName = "Item/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Display")]
    public Sprite icon;

    [Header("Effect")]
    public ItemType type;

    [Tooltip("Flight 타입일 때 비행 지속 시간(초)")]
    public float duration = 15f;
}
