
using UnityEngine;

public class DeadZoneController : MonoBehaviour
{
    [Header("추적 대상 및 설정")]
    public Transform player;
    [SerializeField] private float killY = -20f;
    [SerializeField] private float instantDamage = 9999f;
    
    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
    }
    private void LateUpdate()
    {
        if (player == null) return;
        transform.position = new Vector3(player.position.x, killY, 0f);
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(instantDamage);
        }
    }
}