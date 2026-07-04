using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Stun", story: "[Self] is stunned by [MonsterData]", category: "Action", id: "c905ba70dca7cbe8530ef27a95ce2b96")]
public partial class StunAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<MonsterData> MonsterData;

    private Rigidbody2D _rb;
    private float _elapsed;

    protected override Status OnStart()
    {
        _rb = Self.Value.GetComponent<Rigidbody2D>();
        if (_rb == null || MonsterData.Value == null )
        {
            return Status.Failure;
        }

        _elapsed = 0f;
        _rb.linearVelocity = Vector2.zero;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        //float StunDuration = MonsterData.Value.StunDuration;

        _elapsed += Time.deltaTime;

        if (_elapsed >= MonsterData.Value.StunDuration)
        {
            return Status.Success;
        }

        return Status.Running;
    }
}

