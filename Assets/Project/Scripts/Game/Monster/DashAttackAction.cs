using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "DashAttack", story: "[Self] dashes toward player with [MonsterData]", category: "Action", id: "83e6ea642d1bbae573edbe35dda98b94")]
public partial class DashAttackAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<MonsterData> MonsterData;
    [SerializeReference] public BlackboardVariable<Transform> PlayerTransform;
    
    private Rigidbody2D _rb;
    private Vector2 _dashDirection;
    float _elapsed;

    protected override Status OnStart()
    {
        if (Self.Value == null || PlayerTransform.Value == null)
        {
            return Status.Failure;
        }
        _rb = Self.Value.GetComponent<Rigidbody2D>();
        if (_rb == null)
        {
            return Status.Failure;
        }

        _dashDirection = (PlayerTransform.Value.position - Self.Value.transform.position).normalized;
        _elapsed = 0f;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (_rb == null)
        {
            return Status.Failure;
        }

        _elapsed += Time.deltaTime;

        if (_elapsed <= MonsterData.Value.ChargeDuration)
        {
            _rb.linearVelocity = _dashDirection * MonsterData.Value.ChargeSpeed;

            return Status.Running;
        }


        //float distance = Vector2.Distance(Self.Value.transform.position,
        //    PlayerTransform.Value.position);

        //if (distance <= MonsterData.Value.AttackRange)
        //{
        //    _rb.linearVelocity = Vector2.zero;
        //    return Status.Success;
        //}

        return Status.Success;
    }

    protected override void OnEnd()
    {
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
        }
    }
}

