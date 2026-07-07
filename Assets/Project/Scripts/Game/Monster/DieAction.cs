using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Die", story: "[Monster] dies", category: "Action/Monster/Hit", id: "3291ef21397b1cb1d19ffcc2c152e0b2")]

public partial class DieAction : Action
{
    //[SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<MonsterController> Monster;

    protected override Status OnStart()
    {
        if (Monster.Value == null)
        {
            return Status.Failure;
        }

        Monster.Value.Die();
        return Status.Success;
    }
}

