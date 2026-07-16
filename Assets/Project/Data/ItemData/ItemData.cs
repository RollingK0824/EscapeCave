using UnityEngine;

/// <summary>
/// 인벤토리에 들어가는 아이템의 종류.
/// 새 소모품/효과를 추가할 때 여기에 값만 늘리면 됩니다.
/// </summary>
public enum ItemType
{
    None,
    Flight, // 하늘을 날게 하는 아이템
    SonicBomb, // 마우스로 조준해서 던지는 소리폭탄
    Shield, // 피격 1회를 무효화하는 방패
    Invincibility, // 일정 시간 동안 무적 상태가 되는 아이템
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

    [Tooltip("Flight 타입일 때 비행 지속 시간, Invincibility 타입일 때 무적 지속 시간(초)")]
    public float duration = 15f;

    [Header("Sonic Bomb")]
    [Tooltip("SonicBomb 타입일 때 던져질 투사체 프리팹 (SonicBombProjectile 컴포넌트 필요)")]
    public GameObject projectilePrefab;

    [Tooltip("SonicBomb 타입일 때 조준 방향으로 던지는 속도")]
    public float throwPower = 12f;

    [Tooltip("SonicBomb 타입일 때 충돌 없이도 자동으로 터지기까지 걸리는 시간(초)")]
    public float fuseTime = 1.5f;

    [Tooltip("SonicBomb 타입일 때 터질 때 발생하는 소리의 강도(에코 감지 범위)")]
    public float soundIntensity = 15f;

    [Tooltip("SonicBomb 타입일 때 소리가 퍼져나가는 속도")]
    public float soundSpeed = 6f;
}
