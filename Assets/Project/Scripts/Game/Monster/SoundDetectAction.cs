using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SoundDetect", story: "[Self] detects player by sound within [DetectionRange] and sets [IsDetected]", category: "Action/Monster/Detect", id: "5c947ae7289406428503538c5a5c610b")]
public partial class SoundDetectAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<Transform> PlayerTransform;
    [SerializeReference] public BlackboardVariable<float> DetectionRange;
    [SerializeReference] public BlackboardVariable<bool> IsDetected;

    protected override Status OnUpdate()
    {
        if (Self.Value == null || PlayerTransform.Value == null)
        {
            return Status.Failure;
        }

        ISoundTrigger soundTrigger = Self.Value.GetComponent<ISoundTrigger>();

        if (soundTrigger == null)
        {
            Debug.LogWarning($"[SoundDetect]{Self.Value.name}에 ISoundTrigger 미구현");
            return Status.Failure;
        }

        if (!soundTrigger.IsSoundDetected)
        {
            return Status.Failure;
        }

        float distance = Vector2.Distance(Self.Value.transform.position, PlayerTransform.Value.position);

        if (distance <= DetectionRange.Value)
        {
            IsDetected.Value = true;
            return Status.Success;
        }

        return Status.Failure;
    }
}

