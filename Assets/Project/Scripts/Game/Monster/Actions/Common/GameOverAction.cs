using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "GameOver", story: "[Monster] catches the player", category: "Action/Monster", id: "af90542cfb11725b230d162b0e323a4b")]
public partial class GameOverAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterController> Monster;
    [SerializeReference] public BlackboardVariable<Transform> PlayerTransform;
    [SerializeReference] public BlackboardVariable<bool> IsDetected;

    protected override Status OnStart()
    {
        if (Monster.Value == null || PlayerTransform.Value == null)
        {
            return Status.Failure;
        }

        if (PlayerTransform.Value.TryGetComponent<PlayerInvincibility>(out var invincibility)
            && invincibility.IsInvincible)
        {
            Monster.Value.KnockbackPlayer(PlayerTransform.Value);
            IsDetected.Value = false;
            return Status.Success;
        }

        if (PlayerTransform.Value.TryGetComponent<PlayerController>(out var player))
        {
            player.Die();
        }

        return Status.Success;
    }
}

