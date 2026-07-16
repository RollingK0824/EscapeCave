using System.Collections;
using UnityEngine;

/// <summary>
/// 소리폭탄 투사체. 포물선으로 날아가다가 벽/바닥에 처음 부딪히면 그 뒤로 일정 시간(_impactDelay) 후,
/// 물리(중력/충돌)는 그대로 받는 채로 주기적으로 에코 사운드를 내며 몬스터 어그로를 끈 뒤 풀로 반납됩니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SonicBombProjectile : MonoBehaviour, IEchoable
{
    [Header("애니메이션 (선택)")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _lureAnimationName = "Boom";
    [SerializeField] private string _explodeAnimationName = "Explode";

    private Rigidbody2D _rb;
    private GameObject _sourcePrefab;
    private float _soundIntensity;
    private float _soundSpeed;
    private float _lureDuration;
    private float _pingInterval;
    private float _impactDelay;
    private Coroutine _fuseRoutine;
    private Coroutine _impactRoutine;
    private Coroutine _lureRoutine;
    private bool _hasImpacted;
    private bool _landed;

    public float SoundIntensity => _soundIntensity;
    public float SoundSpeed => _soundSpeed;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }
    }

    /// <summary>
    /// 던져질 때 호출됩니다. sourcePrefab은 풀 반납 시 필요한 원본 프리팹 참조입니다.
    /// </summary>
    public void Launch(GameObject sourcePrefab, Vector2 velocity, float fuseTime, float soundIntensity, float soundSpeed, float lureDuration, float pingInterval, float impactDelay, float spinSpeed)
    {
        _sourcePrefab = sourcePrefab;
        _soundIntensity = soundIntensity;
        _soundSpeed = soundSpeed;
        _lureDuration = lureDuration;
        _pingInterval = pingInterval;
        _impactDelay = impactDelay;
        _hasImpacted = false;
        _landed = false;

        if (_animator != null)
        {
            _animator.speed = 1f;
            _animator.Rebind();
            _animator.Update(0f);
        }

        _rb.bodyType = RigidbodyType2D.Dynamic;
        _rb.linearVelocity = velocity;
        // 던지는 방향(좌/우)에 맞춰 자연스럽게 굴러가는 방향으로 회전을 준다.
        _rb.angularVelocity = velocity.x >= 0f ? -spinSpeed : spinSpeed;

        if (_fuseRoutine != null)
        {
            StopCoroutine(_fuseRoutine);
        }
        if (_impactRoutine != null)
        {
            StopCoroutine(_impactRoutine);
            _impactRoutine = null;
        }
        if (_lureRoutine != null)
        {
            StopCoroutine(_lureRoutine);
            _lureRoutine = null;
        }
        _fuseRoutine = StartCoroutine(FuseRoutine(fuseTime));
    }

    private IEnumerator FuseRoutine(float fuseTime)
    {
        yield return new WaitForSeconds(fuseTime);
        Land();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_hasImpacted) return; // 벽이든 바닥이든 처음 부딪힌 순간만 트리거, 이후 충돌(튕기는 중)은 무시.
        _hasImpacted = true;

        _impactRoutine = StartCoroutine(ImpactRoutine());
    }

    /// <summary>
    /// 처음 부딪힌 시점부터 _impactDelay 만큼 지나면 착지 처리(물리 고정 + 에코/애니메이션)합니다.
    /// 그 사이엔 물리에 그대로 맡겨서 계속 중력을 받으며 튕길 수 있습니다.
    /// </summary>
    private IEnumerator ImpactRoutine()
    {
        yield return new WaitForSeconds(_impactDelay);
        _impactRoutine = null;
        Land();
    }

    /// <summary>
    /// 물리(중력/충돌)는 그대로 두고, 소리/애니메이션 상태만 시작합니다.
    /// </summary>
    private void Land()
    {
        if (_landed) return;
        _landed = true;

        if (_fuseRoutine != null)
        {
            StopCoroutine(_fuseRoutine);
            _fuseRoutine = null;
        }

        PlayLureAnimation();
        _lureRoutine = StartCoroutine(LureRoutine());
    }

    /// <summary>
    /// 지속시간(_lureDuration) 동안 배터리 닳듯 한 번에 다 재생되도록 속도를 맞춰서 처음부터 재생합니다.
    /// </summary>
    private void PlayLureAnimation()
    {
        AnimationClip clip = FindClip(_lureAnimationName);
        if (clip == null || _lureDuration <= 0f) return;

        if (clip.length > 0f)
        {
            _animator.speed = clip.length / _lureDuration;
        }

        _animator.Play(clip.name, 0, 0f);
    }

    private IEnumerator LureRoutine()
    {
        float elapsed = 0f;
        while (elapsed < _lureDuration)
        {
            Echo();
            yield return new WaitForSeconds(_pingInterval);
            elapsed += _pingInterval;
        }

        _lureRoutine = null;
        yield return PlayExplodeAndWait();
        ReturnToPool();
    }

    /// <summary>
    /// 터지는 애니메이션이 있으면 정상 속도로 재생하고 끝날 때까지 기다립니다. 없으면 그냥 넘어갑니다.
    /// </summary>
    private IEnumerator PlayExplodeAndWait()
    {
        AnimationClip clip = FindClip(_explodeAnimationName);
        if (clip == null) yield break;

        _animator.speed = 1f;
        _animator.Play(clip.name, 0, 0f);
        yield return new WaitForSeconds(clip.length);
    }

    private AnimationClip FindClip(string clipName)
    {
        if (_animator == null || string.IsNullOrEmpty(clipName)) return null;

        RuntimeAnimatorController controller = _animator.runtimeAnimatorController;
        if (controller == null || controller.animationClips == null) return null;

        foreach (AnimationClip clip in controller.animationClips)
        {
            if (clip != null && clip.name == clipName) return clip;
        }
        return null;
    }

    private void ReturnToPool()
    {
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

        Managers.MonsterManager.NotifySound(transform, _lureDuration);
    }
}
