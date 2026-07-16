using UnityEngine;

/// <summary>
/// 쉴드 아이템 효과. 활성화되면 다음 피격 1회를 무효화합니다.
/// PlayerController.TakeDamage에서 TryConsumeShield를 확인합니다.
/// </summary>
public class PlayerShield : MonoBehaviour
{
    [Tooltip("쉴드 상태를 나타내는 이펙트 오브젝트 (직접 만든 이펙트). 쉴드가 있는 동안만 켜집니다.")]
    [SerializeField] private GameObject _shieldEffect;

    public bool HasShield { get; private set; }

    private void Awake()
    {
        if (_shieldEffect != null)
        {
            _shieldEffect.SetActive(false);
        }
    }

    public void ActivateShield()
    {
        HasShield = true;
        if (_shieldEffect != null)
        {
            _shieldEffect.SetActive(true);
        }
    }

    /// <summary>
    /// 쉴드가 있다면 소모하고 true를 반환합니다. 없다면 false를 반환합니다.
    /// </summary>
    public bool TryConsumeShield()
    {
        if (!HasShield) return false;

        HasShield = false;
        if (_shieldEffect != null)
        {
            _shieldEffect.SetActive(false);
        }
        return true;
    }
}
