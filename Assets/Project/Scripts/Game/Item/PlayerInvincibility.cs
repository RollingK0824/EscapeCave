using System.Collections;
using UnityEngine;

/// <summary>
/// 무적 아이템 효과. 활성화 후 지정된 시간 동안 IsInvincible이 true가 됩니다.
/// PlayerController.TakeDamage에서 IsInvincible을 확인합니다.
/// </summary>
public class PlayerInvincibility : MonoBehaviour
{
    [Tooltip("무적 상태를 나타내는 이펙트 오브젝트 (직접 만든 반투명 이펙트). 무적 동안만 켜집니다.")]
    [SerializeField] private GameObject _invincibilityEffect;

    private Coroutine _routine;

    public bool IsInvincible { get; private set; }

    private void Awake()
    {
        if (_invincibilityEffect != null)
        {
            _invincibilityEffect.SetActive(false);
        }
    }

    public void StartInvincibility(float duration)
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
        }
        _routine = StartCoroutine(InvincibilityRoutine(duration));
    }

    private IEnumerator InvincibilityRoutine(float duration)
    {
        IsInvincible = true;
        if (_invincibilityEffect != null)
        {
            _invincibilityEffect.SetActive(true);
        }

        yield return new WaitForSeconds(duration);

        IsInvincible = false;
        if (_invincibilityEffect != null)
        {
            _invincibilityEffect.SetActive(false);
        }
        _routine = null;
    }
}
