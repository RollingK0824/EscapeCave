using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AutoEcho", story: "[Monster] periodically echoes", category: "Action/Monster", id: "2530f8fb48e11f9c51ddd53cfd546784")]
public partial class AutoEchoAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterController> Monster;

    protected override Status OnStart()
    {
        if (Monster.Value == null)
        {
            return Status.Failure;
        }

        if (Monster.Value.CurrentState == MonsterState.STUNNED || Monster.Value.CurrentState == MonsterState.DEAD)
        {
            return Status.Success;
        }

        Monster.Value.Echo();

        return Status.Success;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

