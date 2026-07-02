using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "DashAttack", story: "[Self] dashes toward player with [DashSpeed] within [AttackRange]", category: "Action/Monster/Attack", id: "83e6ea642d1bbae573edbe35dda98b94")]
public partial class DashAttackAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<Transform> PlayerTransform;
    [SerializeReference] public BlackboardVariable<float> DashSpeed;
    [SerializeReference] public BlackboardVariable<float> AttackRange;

    private Rigidbody2D _rb;
    private Vector2 _dashDirection;

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

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (_rb == null)
        {
            return Status.Failure;
        }

        _rb.linearVelocity = _dashDirection * DashSpeed.Value;

        float distance = Vector2.Distance(Self.Value.transform.position,
            PlayerTransform.Value.position);
        
        if (distance <= AttackRange.Value)
        {
            _rb.linearVelocity = Vector2.zero;
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
        }
    }
}

