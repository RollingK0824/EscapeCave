using UnityEngine;
using Unity.Behavior;
//using System;
//using Unity.AppUI.Core;
//using UnityEditor.Build.Content;
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

    //public MonsterState CurrentState { get; set; } = MonsterState.IDLE;
    private MonsterState _currentState = MonsterState.IDLE;
    
    public MonsterState CurrentState => _currentState;
    
    public void SetState(MonsterState state)
    {
        _currentState = state;
    }

    public MonsterData Data => _data;

    private Vector2 _lastHitDirection;

    //public event Action<float> OnTrigger1;
    //public event Action<Platform> OnTrigger2;
    //public event Action OnDeath;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        SpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        Animator = GetComponent<Animator>();
        _btAgent = GetComponent<BehaviorGraphAgent>();

        //if (_data != null)
        //{
        //    Rb.gravityScale = _data.GravityScale;
        //}
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        //Transform player = GameManager.Instance.GetPlayer().transform;

        //_btAgent.SetVariableValue("Self", gameObject);
        //_btAgent.SetVariableValue("PlayerTransform", player);
        //_btAgent.SetVariableValue("MoveSpeed", _data.MoveSpeed);
        //_btAgent.SetVariableValue("AttackRange", _data.AttackRange);
        //_btAgent.SetVariableValue("ChargeSpeed", _data.ChargeSpeed);
        //_btAgent.SetVariableValue("ChargeDuration", _data.ChargeDuration);
        //_btAgent.SetVariableValue("StunDuration", _data.StunDuration);
        //_btAgent.SetVariableValue("DetectionRange", _data.DetectionRange);
        //_btAgent.SetVariableValue("AggroResetTime", _data.AggroResetTime);
        //_btAgent.SetVariableValue("WakeUpDuration", _data.WakeUpDuration);
        _btAgent.SetVariableValue("Monster", this );
        _btAgent.SetVariableValue("PlayerTransform", _playerTransform);
        _btAgent.SetVariableValue("SoundTrigger", false);
        _btAgent.SetVariableValue("VibTrigger", false);
        _btAgent.SetVariableValue("IsDetected", false);
        _btAgent.SetVariableValue("IsStunned", false);
        _btAgent.SetVariableValue("IsAwake", false);
        _btAgent.SetVariableValue("IsHit", false);

        //var ts = GameManagerDependencyInfo.Instance.GetTriggerSystem();
        //ts.OnSoundTriggered += HandleTrigger1;
        //ts.OnVibrationTriggered += HandleTrigger2;
    }

    //private void HandleTrigger1(float intensity)
    //{
    //    Transform player = GameManger.Instance.GetPlayer().transform;

    //    if (Vector2.Distacne(transform.position, player.position) <= _data.detectionRange)
    //    {
    //        _btAgent.SetVariableValue("Trigger1Detected", true);
    //    }

    //    OnTrigger1?.Invoke(intensity);
    //}

    //private void HandleTriger2(Platform platform)
    //{
    //    OnTrigger2?.Invoke(platform);
    //}

    public void TakeDamage(int damage, Vector2 hitDirection)
    {
        if (_data.IsInvincible)
        {
            return;
        }

        //StartCoroutine(KnockbackRoutine(hitDirection));
        _lastHitDirection = hitDirection;
        _btAgent.SetVariableValue("IsHit", true);
        SetState(MonsterState.STUNNED);
    }

    public Vector2 GetLastHitDirection() => _lastHitDirection;

    //private IEnumerator KnockbackRoutine(Vector2 hitDirection)
    //{
    //    CurrentState = MonsterState.STUNNED;

    //    Rb.linearVelocity = Vector2.zero;
    //    Rb.AddForce(hitDirection * _data.KnockbackForce, ForceMode2D.Impulse);

    //    yield return new WaitForSeconds(_data.KnockbackDuration);

        //switch (_data.HitReaction)
        //{
        //    case HitReaction.Die:
        //        {
        //            Die();
        //            break;
        //        }
        //    case HitReaction.Stun:
        //        {
        //            Rb.linearVelocity = Vector2.zero;
        //            _btAgent.SetVariableValue("isStunned", true);
        //            CurrentState = MonsterState.STUNNED;
        //            break;
        //        }
        //    case HitReaction.ReturnToChase:
        //        {
        //            Rb.linearVelocity = Vector2.zero;
        //            CurrentState = MonsterState.CHASE;
        //            break;
        //        }
        //}

    //}

    public void SetStunned()
    {
        _btAgent.SetVariableValue("IsStunned", true);
    }

    public void ResetSoundTrigger()
    {
        _btAgent.SetVariableValue("SoundTrigger", false);
    }

    public void ResetVibTrigger()
    {
        _btAgent.SetVariableValue("VibTrigger", false);
    }

    public void Die()
    {
        SetState(MonsterState.DEAD);
        //CurrentState = MonsterState.DEAD;
        _btAgent.enabled = false;
        //OnDeath?.Invoke();

        //var ts = GameManagerDependencyInfo.Instance?.GetTriggerSystem();
        //if (ts != null)
        //{
        //    ts.OnSoundTriggered -= HandleTrigger1;
        //    ts.OnVibrationTriggered -= HandleTrigger2;
        //}

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

    private void OnDestroy()
    {
        //var ts = GameManager.Instance?.GetTriggerSystem();
        //if (ts != null)
        //{
        //    ts.OnSoundTriggered -= HandleTrigger1;
        //    ts.OnVibrationTriggered -= HandleTriger2;
        //}
    }
}