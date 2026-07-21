using UnityEngine;

[RequireComponent(typeof(MonsterController))]
public class MonsterWaterBuoyancy : MonoBehaviour
{
    [SerializeField] private LayerMask _waterLayer;

    private MonsterController _monster;
    private Rigidbody2D _rb;
    private Collider2D _collider;
    private bool _isInWater;

    private void Awake()
    {
        _monster = GetComponent<MonsterController>();
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponentInChildren<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & _waterLayer) == 0)
        {
            return;
        }

        if (_isInWater)
        {
            return;
        }

        if (_collider != null && !_collider.IsTouching(other))
        {
            return;
        }

        _isInWater = true;
        _monster.SetInWater(true);

        _monster.SetGravityScale(_monster.Data.WaterGravityScale);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & _waterLayer) == 0)
        {
            return;
        }

        if (!_isInWater)
        {
            return;
        }

        if (_collider != null && _collider.IsTouching(other))
        {
            return;
        }

        _isInWater = false;
        _monster.SetInWater(false);
        _monster.SetGravityScale(_monster.Data.UseGravity ? _monster.Data.GravityScale : 0f);
    }

    private void FixedUpdate()
    {
        if (_monster.Data.MaxRiseSpeed > 0f && _rb.linearVelocity.y > _monster.Data.MaxRiseSpeed)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _monster.Data.MaxRiseSpeed);
        }
    }
}
