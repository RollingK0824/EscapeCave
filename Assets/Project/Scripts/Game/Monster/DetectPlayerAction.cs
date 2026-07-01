using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "DetectPlayer", story: "[self] detects player within [DetectionRange] using [Trigger1Detected] or [Trigger2Detected]", category: "Action/Monster/Chase", id: "2c2efa4af9362589bccc97718bc14474")]
public partial class DetectPlayerAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<float> DetectionRange;
    [SerializeReference] public BlackboardVariable<bool> Trigger1Detected;
    [SerializeReference] public BlackboardVariable<bool> Trigger2Detected;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

