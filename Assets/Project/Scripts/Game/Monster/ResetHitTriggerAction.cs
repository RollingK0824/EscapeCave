using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ResetHitTrigger", story: "[Monster] resumes after knockback", category: "Action/Monster/Hit", id: "3098f5f6de91dfc93636318e5f246a64")]
public partial class ResetHitTriggerAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterController> Monster;

    protected override Status OnStart()
    {
        if (Monster.Value == null)
        {
            return Status.Failure;
        }

        Monster.Value.ResetHitTrigger();
        return Status.Success;
    }
}

