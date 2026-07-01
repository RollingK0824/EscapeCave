using UnityEngine;
using Unity.Behavior;
using System;
using Unity.AppUI.Core;
using UnityEditor.Build.Content;
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

    public Rigidbody2D Rb { get; private set; }
    public Animator Animator { get; private set; }
    private BehaviorGraphAgent _btAgent;

    public MonsterState CurrentState { get; set; } = MonsterState.IDLE;
    public MonsterData Data => _data;

    public event Action<float> OnTrigger1;
    //public event Action<Platform> OnTrigger2;
    //public event Action OnDeath;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Animator = GetComponent<Animator>();
        _btAgent = GetComponent<BehaviorGraphAgent>();

        if (_data != null)
        {
            Rb.gravityScale = _data.garavityScale;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        //Transform player = GameManager.Instance.GetPlayer().transform;

        _btAgent.SetVariableValue("Self", gameObject);
        //_btAgent.SetVariableValue("PlayerTransform", player);
        _btAgent.SetVariableValue("MoveSpeed", _data.moveSpeed);
        _btAgent.SetVariableValue("AttackRange", _data.attackRange);
        _btAgent.SetVariableValue("ChargeSpeed", _data.chargeSpeed);
        _btAgent.SetVariableValue("ChargeDuration", _data.chargeDuration);
        _btAgent.SetVariableValue("StunDuration", _data.stunDuration);
        _btAgent.SetVariableValue("WakeUpDuration", _data.wakeUpDuration);
        _btAgent.SetVariableValue("DetectionRange", _data.detectionRange);

        _btAgent.SetVariableValue("Trigger1Detected", false);
        _btAgent.SetVariableValue("Trigger2Detected", false);
        _btAgent.SetVariableValue("IsStunned", false);
        _btAgent.SetVariableValue("IsAwake", false);

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
        if (_data.isInvincible)
        {
            return;
        }

        StartCoroutine(KnockbackRoutine(hitDirection));
    }

    private IEnumerator KnockbackRoutine(Vector2 hitDirection)
    {
        CurrentState = MonsterState.STUNNED;

        Rb.linearVelocity = Vector2.zero;
        Rb.AddForce(hitDirection * _data.knockbackForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(_data.knockbackDuration);

        switch (_data.hitReaction)
        {
            case HitReaction.Die:
                {
                    Die();
                    break;
                }
            case HitReaction.Stun:
                {
                    Rb.linearVelocity = Vector2.zero;
                    _btAgent.SetVariableValue("isStunned", true);
                    CurrentState = MonsterState.STUNNED;
                    break;
                }
            case HitReaction.ReturnToChase:
                {
                    Rb.linearVelocity = Vector2.zero;
                    CurrentState = MonsterState.CHASE;
                    break;
                }
        }

    }

    public void SetStunned()
    {
        _btAgent.SetVariableValue("IsStunned", true);
    }

    public void ResetTrigger1()
    {
        _btAgent.SetVariableValue("Trigger1Detected", false);
    }

    public void ResetTrigger2()
    {
        _btAgent.SetVariableValue("Trigger2Detected", false);
    }

    private void Die()
    {
        CurrentState = MonsterState.DEAD;
        _btAgent.enabled = false;
        //OnDeath?.Invoke();

        //var ts = GameManagerDependencyInfo.Instance?.GetTriggerSystem();
        //if (ts != null)
        //{
        //    ts.OnSoundTriggered -= HandleTrigger1;
        //    ts.OnVibrationTriggered -= HandleTrigger2;
        //}

        Destroy(gameObject, 0.5f);
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
