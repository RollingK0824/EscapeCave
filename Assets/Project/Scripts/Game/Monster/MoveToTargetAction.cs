using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "MoveToTarget", story: "[Monster] moves toward [PlayerTransform]", category: "Action", id: "19c7c11385861f34478f465f902c150e")]
public partial class MoveToTargetAction : Action
{
    //[SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<MonsterController> Monster;
    [SerializeReference] public BlackboardVariable<Transform> PlayerTransform;

    protected override Status OnStart()
    {
        if (Monster.Value == null || PlayerTransform.Value == null) 
        {
            return Status.Failure;
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Monster.Value == null || PlayerTransform.Value == null)
        {
            return Status.Failure;
        }

        if (Monster.Value.IsInAttackRange(PlayerTransform.Value))
        {
            Monster.Value.Stop();
            return Status.Success;
        }

        Vector2 direction = Monster.Value.GetDirectionToTarget(PlayerTransform.Value);
        Monster.Value.Move(direction, Monster.Value.Data.MoveSpeed);

        return Status.Running;
    }

    protected override void OnEnd()
    {
        Monster.Value?.Stop();
    }
}

