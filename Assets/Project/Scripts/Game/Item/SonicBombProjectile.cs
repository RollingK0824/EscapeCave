using System.Collections;
using UnityEngine;

/// <summary>
/// 소리폭탄 투사체. 포물선으로 날아가다가 벽/바닥에 부딪히거나
/// 퓨즈 시간이 지나면 그 자리에서 에코 사운드를 발생시키고 풀로 반납됩니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SonicBombProjectile : MonoBehaviour, IEchoable
{
    private Rigidbody2D _rb;
    private GameObject _sourcePrefab;
    private float _soundIntensity;
    private float _soundSpeed;
    private Coroutine _fuseRoutine;
    private bool _detonated;

    public float SoundIntensity => _soundIntensity;
    public float SoundSpeed => _soundSpeed;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// 던져질 때 호출됩니다. sourcePrefab은 풀 반납 시 필요한 원본 프리팹 참조입니다.
    /// </summary>
    public void Launch(GameObject sourcePrefab, Vector2 velocity, float fuseTime, float soundIntensity, float soundSpeed)
    {
        _sourcePrefab = sourcePrefab;
        _soundIntensity = soundIntensity;
        _soundSpeed = soundSpeed;
        _detonated = false;

        _rb.linearVelocity = velocity;
        _rb.angularVelocity = 0f;

        if (_fuseRoutine != null)
        {
            StopCoroutine(_fuseRoutine);
        }
        _fuseRoutine = StartCoroutine(FuseRoutine(fuseTime));
    }

    private IEnumerator FuseRoutine(float fuseTime)
    {
        yield return new WaitForSeconds(fuseTime);
        Detonate();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Detonate();
    }

    private void Detonate()
    {
        if (_detonated) return;
        _detonated = true;

        if (_fuseRoutine != null)
        {
            StopCoroutine(_fuseRoutine);
            _fuseRoutine = null;
        }

        Echo();

        if (Managers.PoolManager.Instance != null && _sourcePrefab != null)
        {
            Managers.PoolManager.Instance.Push(gameObject, _sourcePrefab);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Echo()
    {
        if (Managers.EchoManager.Instance != null)
        {
            Managers.EchoManager.Instance.TriggerSound(transform.position, _soundIntensity, _soundSpeed);
        }
    }
}
