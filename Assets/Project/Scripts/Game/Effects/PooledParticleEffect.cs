using Managers;
using UnityEngine;

/// <summary>
/// 재생이 끝나면 스스로 PoolManager에 반납되는 1회성 파티클 이펙트.
/// 착지 흙먼지, 혓바닥 타격 침 튀김처럼 "스폰 → 한 번 재생 → 반납"하는
/// 모든 파티클 이펙트 프리팹에 공통으로 붙여서 재사용합니다.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class PooledParticleEffect : MonoBehaviour
{
    private ParticleSystem _particleSystem;
    private GameObject _originPrefab;

    private void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
    }

    /// <summary>Pop으로 위치를 옮긴 직후 호출해서 재생을 시작합니다.</summary>
    public void Play(GameObject originPrefab)
    {
        _originPrefab = originPrefab;
        _particleSystem.Clear();
        _particleSystem.Play();
    }

    private void Update()
    {
        if (_originPrefab == null || _particleSystem.IsAlive()) return;

        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Push(gameObject, _originPrefab);
        }
        _originPrefab = null;
    }
}
