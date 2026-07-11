using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "InitMonster", story: "[Self] initializes [Monster] reference", category: "Action/Monster", id: "a5a42c50e2b77c1e486eaf82384741fd")]
public partial class InitMonsterAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<MonsterController> Monster;
    [SerializeReference] public BlackboardVariable<HitReaction> HitReaction;

    protected override Status OnStart()
    {
        if (Self.Value == null)
        {
            return Status.Failure;
        }

        var controller = Self.Value.GetComponent<MonsterController>();
        if (controller == null)
        {
            return Status.Failure;
        }

        Monster.Value = controller;
        HitReaction.Value = controller.Data.HitReaction;

        return Status.Success;
    }
}

