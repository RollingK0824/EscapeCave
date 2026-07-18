using UnityEngine;
using Unity.Behavior;
using System.Collections;
using Managers;
using System;

public enum MonsterState
{
    IDLE,
    ALERT,
    CHASE,
    ATTACK,
    STUNNED,
    DEAD
}

public class MonsterController : MonoBehaviour, IDamageable, IEchoable
{
    [Header("데이터")]
    [SerializeField] private MonsterData _data;

    public Rigidbody2D Rb { get; private set; }
    public SpriteRenderer SpriteRenderer { get; private set; }
    public Animator Animator { get; private set; }
    private BehaviorGraphAgent _btAgent;
    private Collider2D _collider;

    [Header("물리")]
    [SerializeField] private LayerMask _terrainLayer;

    private float _outOfRangeElapsed;
    private bool _isInAttackRangeSticky;
    private Transform _soundSource;
    private float _bombLureExpiry = -1f;

    private MonsterState _currentState = MonsterState.IDLE;
    
    public MonsterState CurrentState => _currentState;
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int IsStunnedHash = Animator.StringToHash("IsStunned");
    private static readonly int AttackHash = Animator.StringToHash("Attack");


    [Header("에코")]
    [SerializeField] private float _soundIntensity;
    [SerializeField] private float _soundSpeed;

    [Header("Sound Effects")]
    [SerializeField] private SoundDataSO _crySound;

    public float SoundIntensity => _soundIntensity;
    public float SoundSpeed => _soundSpeed;


    public void SetState(MonsterState state)
    {
        _currentState = state;
    }

    public MonsterData Data => _data;

    private Vector2 _lastHitDirection;
    private bool _isHitReactiveActive;
    public float CurrentMoveSpeed { get; private set; }


    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        SpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        Animator = GetComponent<Animator>();
        _btAgent = GetComponent<BehaviorGraphAgent>();
        _collider = GetComponentInChildren<Collider2D>();

        _btAgent.SetVariableValue("Monster", this);
        _btAgent.SetVariableValue("SoundTrigger", false);
        _btAgent.SetVariableValue("VibTrigger", false);
        _btAgent.SetVariableValue("IsDetected", false);
        _btAgent.SetVariableValue("IsHit", false);
        _btAgent.SetVariableValue("HitReaction", _data.HitReaction);

        Managers.MonsterManager.RegisterMonster(this);

        CurrentMoveSpeed = Data.MoveSpeed;
    }

    private void Start()
    {
        _btAgent.SetVariableValue("PlayerTransform", Managers.MonsterManager.PlayerTransform);
    }

    public void Move(Vector2 direction, float speed)
    {
        if (_isHitReactiveActive)
        {
            return;
        }

        Rb.linearVelocity = new Vector2(direction.x * speed, Rb.linearVelocity.y);
        FlipSprite(direction);

        SetMoving(true);
    }

    public void FlipSprite(Vector2 direction)
    {
        if (direction.x != 0)
        {
            SpriteRenderer.flipX = direction.x < 0;
        }
    }

    private void SetMoving(bool isMoving)
    {
        Animator.SetBool(IsMovingHash, isMoving);
    }

    private void PlayHit()
    {
        Animator.SetTrigger(HitHash);
    }

    private void PlayDie()
    {
        Animator.SetTrigger(DieHash);
    }

    public void PlayAttack()
    {
        Animator.SetTrigger(AttackHash);
    }

    private void SetStunned(bool isStunned)
    {
        Animator.SetBool(IsStunnedHash, isStunned);
    }

    public void IncreaseMoveSpeed(float deltaTime)
    {
        CurrentMoveSpeed += Data.SpeedRampRate * deltaTime;

        if (Data.MaxMoveSpeed > 0f)
        {
            CurrentMoveSpeed = Mathf.Min(CurrentMoveSpeed, Data.MaxMoveSpeed);
        }
    }

    public void Stop()
    {
        Rb.linearVelocity = new Vector2(0f, Rb.linearVelocity.y);

        SetMoving(false);
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
        if (_isHitReactiveActive)
        {
            return;
        }

        Rb.linearVelocity = direction * speed;
        FlipSprite(direction);

        SetMoving(true);
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

    private static readonly RaycastHit2D[] _wallCastBuffer = new RaycastHit2D[1];

    private bool IsBlocked(Vector2 direction)
    {
        var filter = new ContactFilter2D();
        filter.SetLayerMask(_terrainLayer);
        filter.useTriggers = false;

        int hitCount = _collider.Cast(direction, filter, _wallCastBuffer, 0.1f);

        return hitCount > 0;
    }    

    public void VerticalPatrol(ref int direction, ref Vector2 startPosition)
    {
        if (_isHitReactiveActive)
        {
            return;
        }

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

        SetMoving(true);
    }

    public void HorizontalPatrol(ref int direction)
    {
        if (_isHitReactiveActive)
        {
            return;
        }

        if (IsBlocked(new Vector2(direction, 0)))
        {
            direction *= -1;
        }

        Rb.linearVelocity = new Vector2(direction * Data.PatrolSpeed, Rb.linearVelocity.y);
        FlipSprite(new Vector2(direction, 0));

        SetMoving(true);
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

    public void TakeDamage(float damage)
    {
        if (!_data.IsAttackable)
        {
            return;
        }

        if (_data.IsInvincible)
        {
            return;
        }

        Transform playerTransform = Managers.MonsterManager.PlayerTransform;
        if (playerTransform != null)
        {
             _lastHitDirection = (transform.position - playerTransform.position).normalized;

        }

        PlayHit();

        _btAgent.SetVariableValue("IsHit", true);
        _isHitReactiveActive = true;
    }

    public Vector2 GetLastHitDirection() => _lastHitDirection;

    public void ResetSoundTrigger()
    {
        _btAgent.SetVariableValue("SoundTrigger", false);
    }

    public Transform SoundSource => _soundSource;

    public Transform GetChaseTarget(Transform playerTransform)
    {
        return (Time.time < _bombLureExpiry && _soundSource != null) ? _soundSource : playerTransform;
    }

    public bool IsWithinDetectionRange(Transform source)
    {
        if (source == null)
        {
            return false;
        }

        float distance = Vector2.Distance(transform.position, source.position);

        return distance <= Data.DetectionRange;
    }

    public void ResetVibTrigger()
    {
        _btAgent.SetVariableValue("VibTrigger", false);
    }

    public void ResetHitTrigger()
    {
        _btAgent.SetVariableValue("IsHit", false);
        _isHitReactiveActive = false;
    }

    public void NotifySound(Transform source, float lureDuration)
    {
        _btAgent.SetVariableValue("SoundTrigger", true);

        _soundSource = source;

        if (lureDuration > 0f)
        {
            _bombLureExpiry = Time.time + lureDuration;
        }
    }

    public void NotifyVibration()
    {
        _btAgent.SetVariableValue("VibTrigger", true);
    }

    public void StartStun()
    {
        Stop();
        SetStunned(true);
    }

    public void EndStun()
    {
        SetStunned(false);
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

    private bool _isTouchingPlayer;

    public bool IsTouchingPlayer => _isTouchingPlayer;


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.TryGetComponent<PlayerController>(out _))
        {
            _isTouchingPlayer = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.TryGetComponent<PlayerController>(out _))
        {
            _isTouchingPlayer = false;
        }    
    }

    public void KnockbackPlayer(Transform playerTransform)
    {
        if (playerTransform.TryGetComponent<Rigidbody2D>(out var playerRb))
        {
            Vector2 direction = (playerTransform.position - transform.position).normalized;
            playerRb.AddForce(direction * Data.KnockbackForce, ForceMode2D.Impulse);
        }
    }

    public void Die()
    {
        SetState(MonsterState.DEAD);
        _btAgent.enabled = false;

        SetMoving(false);
        PlayDie();

        GameObject.Destroy(gameObject, Data.DieAnimationDuration);
    }

    public void OnDestroy()
    {
        Managers.MonsterManager.UnregisterMonster(this);
    }

    public void Echo()
    {
        EchoManager.Instance.TriggerSound(transform.position, SoundIntensity, SoundSpeed);

        SoundManager.Instance.PlaySFX(_crySound, transform.position);
    }
}