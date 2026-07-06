using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "KnockbackAction", story: "[Self] receives knockback from [PlayerTransform] with [KnockbackForce] for [KnockbackDuration] seconds", category: "Action/Monster/Hit", id: "d9b1898ce3278d3de8f02988044a7893")]
public partial class KnockbackAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<Transform> PlayerTransform;
    [SerializeReference] public BlackboardVariable<float> KnockbackForce;
    [SerializeReference] public BlackboardVariable<float> KnockbackDuration;

    private Rigidbody2D _rb;
    private float _elapsed;

    protected override Status OnStart()
    {
        _rb = Self.Value.GetComponent<Rigidbody2D>();
        if (_rb == null)
        {
            return Status.Failure;
        }

        Vector2 knockbackDirection = (Self.Value.transform.position - PlayerTransform.Value.position).normalized;

        _elapsed = 0f;
        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(knockbackDirection * KnockbackForce.Value, ForceMode2D.Impulse);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        _elapsed += Time.deltaTime;

        if (_elapsed >= KnockbackDuration.Value)
        {
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

