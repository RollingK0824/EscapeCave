using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "VibrationDetect", story: "[Monster] detects player by vibration and sets [IsDetected]", category: "Action", id: "c26d4dec80107dcea804347f1d9b00b1")]
public partial class VibrationDetectAction : Action
{
    //[SerializeReference] public BlackboardVariable<GameObject> Self;
    //[SerializeReference] public BlackboardVariable<float> DetectionRange;
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
        //IVibrationTrigger vibrationTrigger = Self.Value.GetComponent<IVibrationTrigger>();

        //if (vibrationTrigger == null)
        //{
        //    Debug.LogWarning($"[VibrationDetect]{Self.Value.name}에 IVibrationTrigger 미구현");
        //    return Status.Failure;
        //}

        //if (!vibrationTrigger.IsVibrationDetected)
        //{
        //    return Status.Failure;
        //}
        if (PlayerTransform.Value == null) 
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
            return Status.Success;
        }

        return Status.Failure;
    }
}