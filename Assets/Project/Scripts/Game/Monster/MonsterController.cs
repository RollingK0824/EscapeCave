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

    public bool IsPauseDurationElapsed(float elapsedTime)
    {
        return elapsedTime >= Data.PauseDuration;
    }

    public Vector2 GetDirectionToTarget(Transform playerTransform)
    {
        float dirX = playerTransform.position.x - transform.position.x;
        return new Vector2(Mathf.Sign(dirX), 0f);
    }

    public bool IsChargeDurationElapsed(float elapsedTime)
    {
        return elapsedTime >= Data.ChargeDuration;
    }

    public bool IsInAttackRange(Transform playerTransform)
    {
        float distance = Vector2.Distance(transform.position, playerTransform.position);
        return distance <= Data.AttackRange;
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
                return true;
            }
        }
        else
        {
            _outOfRangeElapsed = 0f;
        }

        return false;
    }

    private void Start()
    {
        _btAgent.SetVariableValue("PlayerTransform", _playerTransform);
    }

    public void TakeDamage(int damage, Vector2 hitDirection)
    {
        if (!_data.IsAttackable)
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

    public void Die()
    {
        SetState(MonsterState.DEAD);
        _btAgent.enabled = false;

        GameObject.Destroy(gameObject);
    }

    public void StartStun()
    {
        Stop();
    }
    
    public bool IsStunFinished(float elapsed)
    {
        return elapsed >= Data.StunDuration;
    }

    public void StartKnockback(Transform playerTransform)
    {
        Vector2 knockbackDirection = (transform.position - playerTransform.position).normalized;
        Stop();
        Rb.AddForce(knockbackDirection * Data.KnockbackForce, ForceMode2D.Impulse);
    }

    public bool IsKnockbackFinished(float elapsed)
    {
        return elapsed >= Data.KnockbackDuration;
    }

    public void EndKnockback()
    {
        Stop();
    }
}