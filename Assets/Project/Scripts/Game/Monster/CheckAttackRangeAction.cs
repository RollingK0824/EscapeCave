using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CheckAttackRange", story: "[Monster] checks if [PlayerTransform] is within attack range", category: "Action/Monster/Chase", id: "d492d4bd9cada61a7b26c86727dde287")]
public partial class CheckAttackRangeAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterController> Monster;
    [SerializeReference] public BlackboardVariable<Transform> PlayerTransform;

    protected override Status OnUpdate()
    {
        if (Monster.Value == null || PlayerTransform.Value == null)
        {
            Debug.Log("[CheckAttackRange] Monster 또는 PlayerTransform이 null입니다");
            return Status.Failure;
        }

        bool inRange = Monster.Value.IsInAttackRange(PlayerTransform.Value);
        Debug.Log($"[CheckAttackRange] 실행 중 - inRange: {inRange}");

        if (inRange)
        {
            return Status.Success;
        }

        return Status.Failure;

        //if (Monster.Value.IsInAttackRange(PlayerTransform.Value))
        //{
        //    return Status.Success;
        //}

        //return Status.Failure;
    }
}

