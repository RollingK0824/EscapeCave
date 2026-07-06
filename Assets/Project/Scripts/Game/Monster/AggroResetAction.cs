using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AggroReset", story: "[Monster] resets aggro after losing player", category: "Action/Monster", id: "233f0aa7b4fe5a84972e220b2256040c")]

public partial class AggroResetAction : Action
{
    //[SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<MonsterController> Monster;
    [SerializeReference] public BlackboardVariable<Transform> PlayerTransform;
    //[SerializeReference] public BlackboardVariable<float> AggroResetTime;
    //[SerializeReference] public BlackboardVariable<float> DetectRange;
    [SerializeReference] public BlackboardVariable<bool> IsDetected;

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

        if (Monster.Value.TryResetAggro(Time.deltaTime, PlayerTransform.Value))
        {
            IsDetected.Value = false;
            return Status.Success;
        }

        return Status.Running;
    }
}

