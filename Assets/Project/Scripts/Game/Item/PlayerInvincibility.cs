using System.Collections;
using UnityEngine;

/// <summary>
/// 무적 아이템 효과. 활성화 후 지정된 시간 동안 IsInvincible이 true가 됩니다.
/// PlayerController.TakeDamage에서 IsInvincible을 확인합니다.
/// </summary>
public class PlayerInvincibility : MonoBehaviour
{
    private Coroutine _routine;

    public bool IsInvincible { get; private set; }

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
        yield return new WaitForSeconds(duration);
        IsInvincible = false;
        _routine = null;
    }
}
