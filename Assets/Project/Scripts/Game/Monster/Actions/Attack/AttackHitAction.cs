using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AttackHit", story: "[Monster] hits [PlayerTransform]", category: "Action", id: "13d3d057aee380b8faef07680a305dd2")]
public partial class AttackHitAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterController> Monster;
    [SerializeReference] public BlackboardVariable<Transform> PlayerTransform;

    protected override Status OnStart()
    {
        if (Monster.Value == null || PlayerTransform.Value == null)
        {
            return Status.Failure;
        }

        Transform target = Monster.Value.GetChaseTarget(PlayerTransform.Value);

        if (target == PlayerTransform.Value && !Monster.Value.IsTouchingPlayer)
        {
            return Status.Failure;
        }

        if (target.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(Monster.Value.Data.AttackDamage);
        }

        return Status.Success;
    }
}

