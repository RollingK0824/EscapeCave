using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Stun", story: "[Monster] is stunned", category: "Action", id: "c905ba70dca7cbe8530ef27a95ce2b96")]
public partial class StunAction : Action
{
    //[SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<MonsterController> Monster;

    private float _elapsed;

    protected override Status OnStart()
    {
        if (Monster.Value == null)
        {
            return Status.Failure;
        }

        _elapsed = 0f;
        Monster.Value.StartStun();

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        //float StunDuration = MonsterData.Value.StunDuration;

        _elapsed += Time.deltaTime;

        if (Monster.Value.IsStunFinished(_elapsed))
        {
            return Status.Success;
        }

        return Status.Running;
    }
}

