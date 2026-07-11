using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "VerticalPatrol", story: "[Monster] patrols vertically", category: "Action/Monster/Patrol", id: "d0f64c0321fce6c3d58efb6dad6347df")]
public partial class VerticalPatrolAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterController> Monster;

    private MonsterController _monster;
    private Vector2 _startPosition;
    private int _direction;

    protected override Status OnStart()
    {
        _monster = Monster.Value;

        if (_monster == null)
        {
            return Status.Failure;
        }

        _startPosition = _monster.transform.position;
        _direction = 1;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        _monster.VerticalPatrol(ref _direction, ref _startPosition);
        return Status.Running;
    }

    protected override void OnEnd()
    {
        _monster?.Stop();
    }
}

