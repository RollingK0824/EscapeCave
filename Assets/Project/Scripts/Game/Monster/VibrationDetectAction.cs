using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "VibrationDetect", story: "[Self] detects player by vibration within [DetectionRange] and sets [IsDetected]", category: "Action", id: "c26d4dec80107dcea804347f1d9b00b1")]
public partial class VibrationDetectAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    //[SerializeReference] public BlackboardVariable<float> DetectionRange;
    [SerializeReference] public BlackboardVariable<bool> IsDetected;
    [SerializeReference] public BlackboardVariable<Transform> PlayerTransform;
    [SerializeReference] public BlackboardVariable<MonsterData> MonsterData;
    [SerializeReference] public BlackboardVariable<bool> VibTrigger;

    protected override Status OnUpdate()
    {
        if (Self.Value == null || PlayerTransform.Value == null || MonsterData.Value == null)
        {
            return Status.Failure;
        }

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

        if (VibTrigger == true)
        {
            float distance = Vector2.Distance(Self.Value.transform.position, PlayerTransform.Value.position);

            if (distance <= MonsterData.Value.DetectionRange)
            {
                IsDetected.Value = true;
                return Status.Success;
            }

        }
       
        return Status.Failure;
    }
}