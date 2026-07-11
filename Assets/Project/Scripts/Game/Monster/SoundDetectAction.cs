using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SoundDetect", story: "[Monster] detects player by sound and sets [IsDetected]", category: "Action/Monster/Detect", id: "5c947ae7289406428503538c5a5c610b")]
public partial class SoundDetectAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterController> Monster;
    [SerializeReference] public BlackboardVariable<Transform> PlayerTransform;
    [SerializeReference] public BlackboardVariable<bool> IsDetected;
    [SerializeReference] public BlackboardVariable<bool> SoundTrigger;

    protected override Status OnStart()
    {
        if (Monster.Value == null)
        {
            return Status.Failure;
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
       if (PlayerTransform.Value == null)
        {
            return Status.Failure;
        }

       if (!Monster.Value.Data.TriggerResponse.HasFlag(TriggerResponse.Sound))
        {
            return Status.Failure;
        }

        if (!SoundTrigger.Value)
        {
            return Status.Failure;
        }
        
        if (Monster.Value.IsPlayerDetectionRange(PlayerTransform.Value))
        {
            IsDetected.Value = true;

            Monster.Value.ResetSoundTrigger();

            return Status.Success;
        }

        return Status.Failure;
    }
}

