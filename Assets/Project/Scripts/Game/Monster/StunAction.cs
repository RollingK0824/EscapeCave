using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Stun", story: "[Self] is stunned for [StunDuration] seconds", category: "Action/Monster/Hit", id: "c905ba70dca7cbe8530ef27a95ce2b96")]
public partial class StunAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<float> StunDuration;

    private Rigidbody2D _rb;
    private float _elapsed;

    protected override Status OnStart()
    {
        _rb = Self.Value.GetComponent<Rigidbody2D>();
        if (_rb == null )
        {
            return Status.Failure;
        }

        _elapsed = 0f;
        _rb.linearVelocity = Vector2.zero;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        _elapsed += Time.deltaTime;

        if (_elapsed >= StunDuration.Value)
        {
            return Status.Success;
        }

        return Status.Running;
    }
}

