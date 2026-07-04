using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CheckAttackRange", story: "[Self] checks if [PlayerTransform] is within [AttackRange]", category: "Action/Monster/Chase", id: "d492d4bd9cada61a7b26c86727dde287")]
public partial class CheckAttackRangeAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<Transform> PlayerTransform;
    [SerializeReference] public BlackboardVariable<float> AttackRange;

    protected override Status OnUpdate()
    {
        if (Self.Value == null || PlayerTransform.Value == null)
        {
            return Status.Failure;
        }

        float distance = Vector2.Distance(Self.Value.transform.position, PlayerTransform.Value.position);

        if (distance <= AttackRange.Value)
        {
            return Status.Success;
        }

        return Status.Failure;
    }
}

