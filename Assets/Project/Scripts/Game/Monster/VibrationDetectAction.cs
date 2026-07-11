using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "VibrationDetect", story: "[Monster] detects player by vibration and sets [IsDetected]", category: "Action/Monster/Detect", id: "c26d4dec80107dcea804347f1d9b00b1")]

public partial class VibrationDetectAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterController> Monster;
    [SerializeReference] public BlackboardVariable<Transform> PlayerTransform;
    [SerializeReference] public BlackboardVariable<bool> IsDetected;
    [SerializeReference] public BlackboardVariable<bool> VibTrigger;

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

        if (!Monster.Value.Data.TriggerResponse.HasFlag(TriggerResponse.Vibration))
        {
            return Status.Failure;
        }

        if (!VibTrigger.Value)
        {
            return Status.Failure;
        }

        if (Monster.Value.IsPlayerDetectionRange(PlayerTransform.Value))
        {
            IsDetected.Value = true;

            Monster.Value.ResetVibTrigger();

            return Status.Success;
        }

        return Status.Failure;
    }
}