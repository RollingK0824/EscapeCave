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

    [Tooltip("SonicBomb 타입일 때 던져질 때 부여할 회전 속도(도/초). 던지는 방향에 따라 자연스럽게 좌우 반전됩니다.")]
    public float spinSpeed = 360f;

    [Tooltip("SonicBomb 타입일 때 벽/바닥 등 아무 곳이든 처음 부딪힌 뒤 착지 처리(에코+애니메이션)되기까지 걸리는 시간(초). " +
             "그동안은 물리적으로 계속 튕길 수 있습니다 (튕기려면 투사체 프리팹 Collider에 반발력 있는 Physics Material 2D 필요).")]
    public float impactDelay = 1f;

    [Tooltip("SonicBomb 타입일 때 충돌 없이도 자동으로 착지 처리되기까지 걸리는 시간(초). 실제 던지기 궤적이 끝나기 전에 발동하지 않도록 넉넉하게 잡아야 합니다.")]
    public float fuseTime = 5f;

    [Tooltip("SonicBomb 타입일 때 착지 후 반복해서 내는 소리 한 번의 강도(에코 감지 범위)")]
    public float soundIntensity = 15f;

    [Tooltip("SonicBomb 타입일 때 소리가 퍼져나가는 속도")]
    public float soundSpeed = 6f;

    [Tooltip("SonicBomb 타입일 때 착지 후 소리를 반복해서 내며 어그로를 끄는 총 지속 시간(초)")]
    public float lureDuration = 3f;

    [Tooltip("SonicBomb 타입일 때 착지 후 소리를 반복하는 간격(초)")]
    public float pingInterval = 0.5f;
}
