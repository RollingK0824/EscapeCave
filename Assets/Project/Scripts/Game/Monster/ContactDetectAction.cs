using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ContactDetect", story: "[Monster] detects [PlayerTransform] via collider contact", category: "Action/Monster", id: "ab2fe1790e587cf0aebe39ba9fe4ef23")]
public partial class ContactDetectAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterController> Monster;
    [SerializeReference] public BlackboardVariable<Transform> PlayerTransform;
    [SerializeReference] public BlackboardVariable<bool> IsDetected;

    protected override Status OnStart()
    {
        if (Monster.Value == null)
        {
            return Status.Failure;
        }

        if (Monster.Value.IsTouchingPlayer)
        {
            IsDetected.Value = true;
            return Status.Success;
        }

        return Status.Failure;
    }
}

