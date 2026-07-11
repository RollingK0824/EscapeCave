using UnityEngine;
using Unity.Behavior;
using System.Collections;

public enum MonsterState
{
    IDLE,
    ALERT,
    CHASE,
    ATTACK,
    STUNNED,
    DEAD
}

public class MonsterController : MonoBehaviour
{
    [Header("데이터")]
    [SerializeField] private MonsterData _data;
    [SerializeField] private Transform _playerTransform;

    public Rigidbody2D Rb { get; private set; }
    public SpriteRenderer SpriteRenderer { get; private set; }
    public Animator Animator { get; private set; }
    private BehaviorGraphAgent _btAgent;

    private float _outOfRangeElapsed;
    private bool _isInAttackRangeSticky;

    private MonsterState _currentState = MonsterState.IDLE;
    
    public MonsterState CurrentState => _currentState;

    public void SetState(MonsterState state)
    {
        _currentState = state;
    }

    public MonsterData Data => _data;

    private Vector2 _lastHitDirection;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        SpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        Animator = GetComponent<Animator>();
        _btAgent = GetComponent<BehaviorGraphAgent>();

        _btAgent.SetVariableValue("Monster", this);
        _btAgent.SetVariableValue("PlayerTransform", _playerTransform);
        _btAgent.SetVariableValue("SoundTrigger", false);
        _btAgent.SetVariableValue("VibTrigger", false);
        _btAgent.SetVariableValue("IsDetected", false);
        _btAgent.SetVariableValue("IsHit", false);
        _btAgent.SetVariableValue("HitReaction", _data.HitReaction);
    }

    public void Move(Vector2 direction, float speed)
    {
        Rb.linearVelocity = new Vector2(direction.x * speed, Rb.linearVelocity.y);
        FlipSprite(direction);
    }

    public void FlipSprite(Vector2 direction)
    {
        if (direction.x != 0)
        {
            SpriteRenderer.flipX = direction.x < 0;
        }
    }

    public void Stop()
    {
        Rb.linearVelocity = new Vector2(0f, Rb.linearVelocity.y);
    }

    public Vector2 GetDirectionToTarget(Transform playerTransform)
    {
        float dirX = playerTransform.position.x - transform.position.x;
        if (Mathf.Abs(dirX) < 0.05f)
        {
            return Vector2.zero;
        }
        return new Vector2(Mathf.Sign(dirX), 0f);
    }

    public float GetDistanceToTarget(Transform playerTransform)
    {
        return Vector2.Distance(transform.position, playerTransform.position);
    }

    public Vector2 GetTargetPoint(Transform target)
    {
        var col = target.GetComponent<Collider2D>();
        return col != null ? (Vector2)col.bounds.center : (Vector2)target.position;
    }

    public Vector2 GetDirectionToTargetFull(Transform playerTransform)
    {
        Vector2 targetPoint = GetTargetPoint(playerTransform);
        Vector2 diff = targetPoint - (Vector2)transform.position;
        return diff.sqrMagnitude > 0f ? diff.normalized : Vector2.zero;
    }

    public void MoveFreely(Vector2 direction, float speed)
    {
        Rb.linearVelocity = direction * speed;
        FlipSprite(direction);
    }

    public void MoveTowardTarget(Transform target, float speed)
    {
        if (Data.FliesFreely)
        {
            MoveFreely(GetDirectionToTargetFull(target), speed);
        }
        else
        {
            Move(GetDirectionToTarget(target), speed);
        }
    }

    public Vector2 GetChargeDirection(Transform target)
    {
        return Data.FliesFreely ? GetDirectionToTargetFull(target) : GetDirectionToTarget(target);
    }    

    public void MoveAlongDirection(Vector2 direction, float speed)
    {
        if (Data.FliesFreely)
        {
            MoveFreely(direction, speed);
        }
        else
        {
            Move(direction, speed);
        }
    }

    public void VerticalPatrol(ref int direction, ref Vector2 startPosition)
    {

        float delta = transform.position.y - startPosition.y;

        if (delta >= Data.PatrolRange)
        {
            direction = -1;
        }
        else if (delta <= -Data.PatrolRange)
        {
            direction = 1;
        }

        Rb.linearVelocity = new Vector2(Rb.linearVelocity.x, direction * Data.PatrolSpeed);
    }

    public void HorizontalPatrol(ref int direction, ref Vector2 startPosition)
    {
        float delta = transform.position.x - startPosition.x;

        if (delta >= Data.PatrolRange)
        {
            direction = -1;
        }
        else if (delta <= -Data.PatrolRange)
        {
            direction = 1;
        }

        Rb.linearVelocity = new Vector2(direction * Data.PatrolSpeed, Rb.linearVelocity.y);
        FlipSprite(new Vector2(direction, 0));
    }

    public bool IsPlayerDetectionRange(Transform playerTransform)
    {
        float distance = Vector2.Distance(transform.position, playerTransform.position);
        return distance <= Data.DetectionRange;
    }

    public bool TryResetAggro(float deltaTime, Transform playerTransform)
    {
        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (distance > Data.DetectionRange)
        {
            _outOfRangeElapsed += deltaTime;

            if (_outOfRangeElapsed >= Data.AggroResetTime)
            {
                _outOfRangeElapsed = 0f;
                _isInAttackRangeSticky = false;
                return true;
            }
        }
        else
        {
            _outOfRangeElapsed = 0f;
        }

        return false;
    }

    public bool IsPauseDurationElapsed(float elapsedTime)
    {
        return elapsedTime >= Data.PauseDuration;
    }

    public bool IsChargeDurationElapsed(float elapsedTime)
    {
        return elapsedTime >= Data.ChargeDuration;
    }

    public bool IsInAttackRange(Transform playerTransform)
    {
        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (_isInAttackRangeSticky)
        {
            _isInAttackRangeSticky = distance <= Data.AttackRange + Data.AttackRangeExitBuffer;
        }
        else
        {
            _isInAttackRangeSticky = distance <= Data.AttackRange;
        }

        return _isInAttackRangeSticky;
    }

    public bool IsStunFinished(float elapsed)
    {
        return elapsed >= Data.StunDuration;
    }

    public bool IsKnockbackFinished(float elapsed)
    {
        return elapsed >= Data.KnockbackDuration;
    }

    public void TakeDamage(int damage, Vector2 hitDirection)
    {
        if (!_data.IsAttackable)
        {
            return;
        }

        if (_data.IsInvincible)
        {
            return;
        }

        _lastHitDirection = hitDirection;
        _btAgent.SetVariableValue("IsHit", true);
    }

    public Vector2 GetLastHitDirection() => _lastHitDirection;

    public void ResetSoundTrigger()
    {
        _btAgent.SetVariableValue("SoundTrigger", false);
    }

    public void ResetVibTrigger()
    {
        _btAgent.SetVariableValue("VibTrigger", false);
    }

    public void ResetHitTrigger()
    {
        _btAgent.SetVariableValue("IsHit", false);
    }

    public void StartStun()
    {
        Stop();
    }

    public void StartKnockback(Transform playerTransform)
    {
        Vector2 knockbackDirection = (transform.position - playerTransform.position).normalized;
        Stop();
        Rb.AddForce(knockbackDirection * Data.KnockbackForce, ForceMode2D.Impulse);
    }

    public void EndKnockback()
    {
        Stop();
    }

    public void Die()
    {
        SetState(MonsterState.DEAD);
        _btAgent.enabled = false;

        GameObject.Destroy(gameObject);
    }
}