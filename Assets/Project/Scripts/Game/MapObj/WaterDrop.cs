using Managers;
using UnityEngine;

/// <summary>
/// 종유석에서 떨어지는 물방울 오브젝트.
/// 바닥/플랫폼 등에 닿으면 에코 파동을 발생시키고 오브젝트 풀로 반납됩니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class WaterDrop : MonoBehaviour, IEchoable
{
    [Header("에코 사운드 설정")]
    [SerializeField] private float _soundIntensity = 10f;
    [SerializeField] private float _soundSpeed = 5f;

    [Header("충돌 대상 레이어 (바닥 및 플랫폼)")]
    [SerializeField] private LayerMask _collisionLayers;

    [HideInInspector] public int poolKey;

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

        if (Managers.PoolManager.Instance != null)
        {
            Managers.PoolManager.Instance.Push(gameObject, poolKey);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
