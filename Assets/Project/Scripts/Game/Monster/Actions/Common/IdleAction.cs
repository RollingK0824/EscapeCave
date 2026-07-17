using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Idle", story: "[Monster] idels until detected", category: "Action/Monster", id: "c7d3ba6a870e0543a489199c486e10de")]
public partial class IdleAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterController> Monster;

    protected override Status OnStart()
    {
        if (Monster.Value == null)
        {
            return Status.Failure;
        }

        Monster.Value.Stop();
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }
}

