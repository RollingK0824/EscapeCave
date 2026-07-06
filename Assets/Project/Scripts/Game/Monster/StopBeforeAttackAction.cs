using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "StopBeforeAttack", story: "[Monster] stops before attacking", category: "Action/Monster/Attack", id: "fd37ca42bd56ed420a1a10d0ec0404cf")]
public partial class StopBeforeAttackAction : Action
{
    //[SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<MonsterController> Monster;
    //[SerializeReference] public BlackboardVariable<MonsterData> MonsterData;

    private float _elapsedTime;

    protected override Status OnStart()
    {
        if (Monster.Value == null)
        {
            return Status.Failure;
        }

        _elapsedTime = 0f;
        Monster.Value.Stop();

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        _elapsedTime += Time.deltaTime;

        if (Monster.Value.IsPauseDurationElapsed(_elapsedTime))
        {
            return Status.Success;
        }

        return Status.Running;
    }
}