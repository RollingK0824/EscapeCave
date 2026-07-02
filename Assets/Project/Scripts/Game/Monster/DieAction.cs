using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Die", story: "[Self] dies", category: "Action/Monster/Hit", id: "3291ef21397b1cb1d19ffcc2c152e0b2")]
public partial class DieAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    protected override Status OnStart()
    {
        if (Self.Value == null)
        {
            return Status.Failure;
        }

        GameObject.Destroy(Self.Value);
        return Status.Success;
    }
}

