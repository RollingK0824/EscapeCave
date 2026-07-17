using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "IsTrue", story: "[Value] is true", category: "Action/Common", id: "e0f6eda36ec057101759622214e4532d")]
public partial class IsTrueAction : Action
{
    [SerializeReference] public BlackboardVariable<bool> Value;

    protected override Status OnUpdate()
    {
        return Value.Value ? Status.Success : Status.Failure;
    }
}

