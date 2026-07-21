using Managers;
using UnityEngine;

/// <summary>
/// 종유석에서 떨어지는 물방울 오브젝트.
/// 바닥/플랫폼 등에 닿으면 에코 파동을 발생시키고 오브젝트 풀로 반납됩니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class TutorialWaterDrop : MonoBehaviour, IEchoable
{
    [Header("에코 사운드 설정")]
    [SerializeField] private float _soundIntensity = 10f;
    [SerializeField] private float _soundSpeed = 5f;

    [Header("충돌 대상 레이어 (바닥 및 플랫폼)")]
    [SerializeField] private LayerMask _collisionLayers;

    [SerializeField, Tooltip("바닥에 튕길 때 재생할 물방울 튀김 이펙트 프리팹 (PooledParticleEffect 필요, PlayerTongueAttack의 splash 이펙트 재활용 가능)")]
    private GameObject _splashEffectPrefab;

    [HideInInspector] public int poolKey;

    public event System.Action OnLanded;

    private Rigidbody2D _rb;

    public float SoundIntensity => _soundIntensity;
    public float SoundSpeed => _soundSpeed;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }
    }

    public void Echo()
    {
        if (Managers.EchoManager.Instance != null)
        {
            Managers.EchoManager.Instance.TriggerSound(transform.position, SoundIntensity, SoundSpeed);
            SoundManager.Instance.PlaySFX("WaterDrip", transform.position);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & _collisionLayers) != 0)
        {
            HandleCollision();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & _collisionLayers) != 0)
        {
            HandleCollision();
        }
    }

    private void HandleCollision()
    {
        Echo();
        OnLanded?.Invoke();
        SpawnSplashEffect();

        if (Managers.PoolManager.Instance != null)
        {
            Managers.PoolManager.Instance.Push(gameObject, poolKey);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SpawnSplashEffect()
    {
        if (_splashEffectPrefab == null || Managers.PoolManager.Instance == null)
        {
            return;
        }

        GameObject fx = Managers.PoolManager.Instance.Pop(_splashEffectPrefab, transform.position, Quaternion.identity);
        if (fx == null)
        {
            return;
        }

        if (fx.TryGetComponent<PooledParticleEffect>(out var pooledEffect))
        {
            pooledEffect.Play(_splashEffectPrefab);
        }
        else
        {
            Debug.LogWarning($"{nameof(TutorialWaterDrop)}: {_splashEffectPrefab.name}에 PooledParticleEffect 컴포넌트가 없어서 자동으로 반납되지 않습니다.", _splashEffectPrefab);
        }
    }
}
