using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "DashAttack", story: "[Monster] dashes toward player", category: "Action", id: "83e6ea642d1bbae573edbe35dda98b94")]
public partial class DashAttackAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterController> Monster;
    [SerializeReference] public BlackboardVariable<Transform> PlayerTransform;
    
    private Vector2 _dashDirection;
    float _elapsed;

    protected override Status OnStart()
    {
        if (Monster.Value == null || PlayerTransform.Value == null)
        {
            return Status.Failure;
        }

        _dashDirection = Monster.Value.GetChargeDirection(Monster.Value.GetChaseTarget(PlayerTransform.Value));
        _elapsed = 0f;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        _elapsed += Time.deltaTime;

        float distanceRemaining = Monster.Value.GetDistanceToTarget(Monster.Value.GetChaseTarget(PlayerTransform.Value));

        if (!Monster.Value.IsChargeDurationElapsed(_elapsed) && distanceRemaining > Monster.Value.Data.AttackRange)
        {
            Monster.Value.MoveAlongDirection(_dashDirection, Monster.Value.Data.ChargeSpeed);
            return Status.Running;
        }

        return Status.Success;
    }

    protected override void OnEnd()
    {
        Monster.Value?.Stop();
    }
}

