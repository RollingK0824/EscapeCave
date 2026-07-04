using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AggroReset", story: "[Self] resets aggro after [AggroResetTime] seconds out of [DetectRange]", category: "Action", id: "233f0aa7b4fe5a84972e220b2256040c")]
public partial class AggroResetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<Transform> PlayerTransform;
    [SerializeReference] public BlackboardVariable<float> AggroResetTime;
    [SerializeReference] public BlackboardVariable<float> DetectRange;
    [SerializeReference] public BlackboardVariable<bool> IsDetected;

    private float _outOfRangeElapsed;

    protected override Status OnStart()
    {
        _outOfRangeElapsed = 0;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (PlayerTransform.Value == null)
        {
            return Status.Failure;
        }

        float distance = Vector2.Distance(Self.Value.transform.position, PlayerTransform.Value.position);

        if (distance > DetectRange.Value)
        {
            _outOfRangeElapsed += Time.deltaTime;

            if (_outOfRangeElapsed >= AggroResetTime.Value)
            {
                IsDetected.Value = true;
                return Status.Success;
            }
        }
        else
        {
            _outOfRangeElapsed = 0f;
        }

        return Status.Running;
    }

}

