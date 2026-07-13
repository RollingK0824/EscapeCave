using Managers;
using UnityEngine;

/// <summary>
/// 착지 순간 발밑에 흙먼지 이펙트를 재생합니다. PlayerJump의 OnLanded 이벤트만 구독하며,
/// 점프 물리 자체에는 관여하지 않습니다.
/// </summary>
[RequireComponent(typeof(PlayerJump))]
public class PlayerLandingEffect : MonoBehaviour
{
    [SerializeField] private GameObject _landDustPrefab;
    [SerializeField, Tooltip("플레이어 기준 이펙트가 나올 위치 (보통 발밑)")]
    private Vector3 _spawnOffset = new Vector3(0f, -0f, 0f);

    private PlayerJump _jump;

    private void Awake()
    {
        _jump = GetComponent<PlayerJump>();
        _jump.OnLanded += HandleLanded;
    }

    private void OnDestroy()
    {
        if (_jump != null)
            _jump.OnLanded -= HandleLanded;
    }

    private void HandleLanded()
    {
        if (_landDustPrefab == null || PoolManager.Instance == null) return;

        GameObject fx = PoolManager.Instance.Pop(_landDustPrefab, transform.position + _spawnOffset, Quaternion.identity);
        if (fx == null) return;

        if (fx.TryGetComponent<PooledAnimationEffect>(out var pooledEffect))
        {
            pooledEffect.Play(_landDustPrefab);
        }
        else
        {
            Debug.LogWarning($"{nameof(PlayerLandingEffect)}: {_landDustPrefab.name}에 PooledAnimationEffect 컴포넌트가 없어서 자동으로 반납되지 않습니다.", _landDustPrefab);
        }
    }
}
