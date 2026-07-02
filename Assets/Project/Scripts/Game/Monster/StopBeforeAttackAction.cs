using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "StopBeforeAttack", story: "[Self] stops for [StopDuration] seconds before attacking", category: "Action/Monster/Attack", id: "fd37ca42bd56ed420a1a10d0ec0404cf")]
public partial class StopBeforeAttackAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<float> StopDuration;

    private float _elapsedTime;

    protected override Status OnStart()
    {
        _elapsedTime = 0f;

        var rb = Self.Value.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        _elapsedTime += Time.deltaTime;

        if (_elapsedTime >= StopDuration.Value)
        {
            return Status.Success;
        }

        return Status.Running;
    }
}

