using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AccelerateSpeed", story: "[Monster] speeds up over time", category: "Action/Monster", id: "d1d367ead1a6538897bf02632b580811")]
public partial class AccelerateSpeedAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterController> Monster;

    protected override Status OnUpdate()
    {
        if (Monster.Value == null)
        {
            return Status.Failure;
        }

        Monster.Value.IncreaseMoveSpeed(Time.deltaTime);
        return Status.Running;
    }
}

