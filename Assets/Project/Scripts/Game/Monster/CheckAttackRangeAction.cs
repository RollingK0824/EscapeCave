using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CheckAttackRange", story: "[Monster] checks if [PlayerTransform] is within attack range", category: "Action/Monster/Attack", id: "d492d4bd9cada61a7b26c86727dde287")]
public partial class CheckAttackRangeAction : Action
{
    //[SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<MonsterController> Monster;
    [SerializeReference] public BlackboardVariable<Transform> PlayerTransform;

    protected override Status OnUpdate()
    {
        if (Monster.Value == null || PlayerTransform.Value == null)
        {
            return Status.Failure;
        }

        if (Monster.Value.IsInAttackRange(PlayerTransform.Value))
        {
            return Status.Success;
        }

        return Status.Failure;
    }
}

