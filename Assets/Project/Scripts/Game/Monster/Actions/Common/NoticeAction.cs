using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Notice", story: "[Monster] notices the player", category: "Action/Monster", id: "e03a8b2758df70977cd5ea4f33167810")]
public partial class NoticeAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterController> Monster;

    private float _elapsed;

    protected override Status OnStart()
    {
        if (Monster.Value == null)
        {
            return Status.Failure;
        }

        _elapsed = 0f;
        Monster.Value.Stop();

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        _elapsed += Time.deltaTime;

        return _elapsed >= Monster.Value.Data.NoticeDuration ? Status.Success : Status.Running;
    }
}

