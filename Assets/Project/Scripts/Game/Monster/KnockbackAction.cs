using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "KnockbackAction", story: "[Monster] receives knockback from [PlayerTransform]", category: "Action/Monster/Hit", id: "d9b1898ce3278d3de8f02988044a7893")]
public partial class KnockbackAction : Action
{
    //[SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<MonsterController> Monster;
    [SerializeReference] public BlackboardVariable<Transform> PlayerTransform;
    //[SerializeReference] public BlackboardVariable<float> KnockbackForce;
    //[SerializeReference] public BlackboardVariable<float> KnockbackDuration;

    private float _elapsed;

    protected override Status OnStart()
    {
        if (Monster.Value == null || PlayerTransform.Value == null)
        {
            return Status.Failure;
        }

        _elapsed = 0f;
        Monster.Value.StartKnockback(PlayerTransform.Value);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        _elapsed += Time.deltaTime;

        if (Monster.Value.IsKnockbackFinished(_elapsed))
        {
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        Monster.Value?.EndKnockback();
        Monster.Value?.ResetHitTrigger();
    }
}

