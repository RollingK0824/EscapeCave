using UnityEngine;

/// <summary>
/// 쉴드 아이템 효과. 활성화되면 다음 피격 1회를 무효화합니다.
/// PlayerController.TakeDamage에서 TryConsumeShield를 확인합니다.
/// </summary>
public class PlayerShield : MonoBehaviour
{
    public bool HasShield { get; private set; }

    public void ActivateShield()
    {
        HasShield = true;
    }

    /// <summary>
    /// 쉴드가 있다면 소모하고 true를 반환합니다. 없다면 false를 반환합니다.
    /// </summary>
    public bool TryConsumeShield()
    {
        if (!HasShield) return false;

        HasShield = false;
        return true;
    }
}
